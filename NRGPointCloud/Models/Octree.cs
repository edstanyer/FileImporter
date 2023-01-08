using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NRG.Services;
using NRG.Import;
using NRG.Export;
using NRG.MathsHelpers;
using NRG.Models.Rendering;

namespace NRG.Models
{
    public class Converter
    {
        #region Properties

        private List<string> sources;
        private List<AsciiFormat> sourceFormats;
        private string workDir;
        private string projectName = "";
        private string projectPath = "";
        private double spacing;
        private List<double> colorRange = new List<double>();
        private List<double> intensityRange = new List<double>();
        private double scale = 0;
        private int diagonalFraction = 200;
        private List<ulong> pointsList = new List<ulong>();
        
        #endregion

        #region Setup

        public Converter(string workDir, string projectName, string projectPath, List<string> sources)
        {
            this.workDir = workDir;
            this.projectName = projectName;
            this.projectPath = projectPath;
            this.sources = sources;
        }

        public Converter(string workDir, string projectName, string projectPath, List<string> sources, List<AsciiFormat> formats)
        {
            this.workDir = workDir;
            this.projectName = projectName;
            this.projectPath = projectPath;
            this.sources = sources;
            this.sourceFormats = formats;
        }

        #endregion

        #region Methods

        public void Convert(BackgroundWorker worker, ref DoWorkEventArgs e)
        {
            Prepare();

            var state = new ConversionProgress();
            state.Stage = ConversionStage.GetBounds;
            worker.ReportProgress(0, state);

            ulong pointsProcessed = 0;
            ulong NextProcessCheck = 10000000;
            
            var bounds = CalculateBounds(worker, ref e);
            if(worker.CancellationPending)
            {
                e.Cancel = true;
                return;
            }

            if (scale == 0)
            {
                //if (bounds.Size.Length() > 1000000)
                //    scale = 0.01;
                //else if (bounds.Size.Length() > 100000)
                //    scale = 0.001;
                //else if (bounds.Size.Length() > 1)
                //    scale = 0.001;
                //else
                    scale = 0.0001;
            }

            bounds.MakeCubic();

            if (diagonalFraction != 0)
                spacing = bounds.Size.Length() / diagonalFraction;

            Octree octree = null;
            octree = new Octree(this.workDir, this.projectName, this.projectPath, bounds, spacing, -1, scale);
            octree.Version = OctreeFileVersion.FileVersion2;

            ulong totalPoints = 0;
            foreach (var count in pointsList)
                totalPoints += count;

            if (octree == null)
                return;

            //Process the files
            pointsProcessed = 0;
            foreach (var source in sources)
            {
                ulong localPointsProcessed = 0;
                var numPoints = pointsList[sources.IndexOf(source)];

                //Report conversion process
                state.Stage = ConversionStage.BuildingOctree;
                state.PrimaryPercentage = (int)(100 * ((double)sources.IndexOf(source) / (double)sources.Count));
                state.SecondaryPercentrage = 0;
                state.SecondaryMessage = Path.GetFileName(source);
                state.TotalPoints = totalPoints;
                state.CurrentPoints = pointsProcessed;
                state.TotalFilePoints = numPoints;
                state.currentFilePoints = localPointsProcessed;
                worker.ReportProgress(0, state);

                PointReader reader = null;

                if (sourceFormats != null && sourceFormats.Count >= 0)
                    reader = CreatePointReader(source, sourceFormats[sources.IndexOf(source)]);
                else
                    reader = CreatePointReader(source);

                double fraction = 1.0D / (double)numPoints;
                var lastProgress = 0;

                CloudPoint p = null;

                while (reader.ReadNextPoint(ref p))
                {
                    localPointsProcessed++;
                    pointsProcessed++;

                    octree.Add(p);
                    if ((int)(localPointsProcessed * fraction * 100) > lastProgress)
                    {
                        if(worker.CancellationPending)
                        {
                            octree.WaitUntillProcessed();
                            reader.Close();
                            reader = null;
                            e.Cancel = true;
                            return;
                        }

                        lastProgress = (int)(localPointsProcessed * fraction * 100);
                        state.CurrentPoints = pointsProcessed;
                        state.currentFilePoints = localPointsProcessed;
                        state.SecondaryPercentrage = lastProgress;
                        worker.ReportProgress(0, state);
                    }

                    if (pointsProcessed == NextProcessCheck)
                    {
                        if (worker.CancellationPending)
                        {
                            octree.WaitUntillProcessed();
                            reader.Close();
                            reader = null;
                            e.Cancel = true;
                            return;
                        }

                        NextProcessCheck += 10000000;
                        octree.WaitUntillProcessed();
                        state.Stage = ConversionStage.FlushingOctree;
                        worker.ReportProgress(0, state);
                        octree.Flush();
                        state.Stage = ConversionStage.BuildingOctree;
                        worker.ReportProgress(0, state);
                    }
                }

                reader.Close();
                reader = null;
            }

            state.Stage = ConversionStage.FinalizingOctree;
            state.CurrentPoints = pointsProcessed;
            worker.ReportProgress(0, state);
            octree.WaitUntillProcessed();
            octree.Flush();
        }

        public void MergePointCloud(BackgroundWorker worker, Octree currentOctree, ref DoWorkEventArgs e)
        {
            Prepare();

            var state = new ConversionProgress();
            state.Stage = ConversionStage.GetBounds;
            worker.ReportProgress(0, state);

            ulong pointsProcessed = 0;
            ulong nextProcessCheck = 10000000;
            var bounds = CalculateBounds(worker, ref e);

            //Add point cloud bounds
            bounds.Update(currentOctree.TightBounds);

            if(scale == 0)
            {
                if (bounds.Size.Length() > 1000000)
                    scale = 0.01;
                else if (bounds.Size.Length() > 100000)
                    scale = 0.001;
                else if (bounds.Size.Length() > 1)
                    scale = 0.001;
                else
                    scale = 0.0001;
            }

            bounds.MakeCubic();

            if (diagonalFraction != 0)
                spacing = bounds.Size.Length() / diagonalFraction;

            Octree octree = null;
            octree = new Octree(this.workDir, this.projectName, this.projectPath, bounds, spacing, -1, scale);
            octree.Version = OctreeFileVersion.FileVersion2;

            if (octree == null)
                return;

            //Process the point cloud
            var nodes = new List<OctreeNode>();
            currentOctree.Root.GetAllNodes(ref nodes);
            ulong totalPoints = 0;
            foreach (var count in pointsList)
                totalPoints += count;

            totalPoints += currentOctree.NumAccepted;

            //Report conversion progress
            state.Stage = ConversionStage.BuildingOctree;
            state.PrimaryPercentage = 0;
            state.SecondaryPercentrage = 0;
            state.SecondaryMessage = Path.GetFileNameWithoutExtension(currentOctree.Name);
            state.TotalPoints = totalPoints;
            state.CurrentPoints = pointsProcessed;
            state.TotalFilePoints = currentOctree.NumAccepted;
            state.currentFilePoints = 0;
            worker.ReportProgress(0, state);

            ulong localPointsProcessed = 0;
            var numPoints = currentOctree.NumAccepted;
            double fraction = 1.0D / numPoints;

            foreach (var node in nodes)
            {
                var points = node.LoadIntoStore();

                var lastProgress = 0;

                foreach(var point in points)
                {
                    localPointsProcessed++;
                    pointsProcessed++;

                    octree.Add(point);
                    if ((int)(localPointsProcessed * fraction * 100) > lastProgress)
                    {
                        if(worker.CancellationPending)
                        {
                            octree.WaitUntillProcessed();
                            e.Cancel = true;
                            return;
                        }

                        lastProgress = (int)(localPointsProcessed * fraction * 100);
                        state.CurrentPoints = pointsProcessed;
                        state.currentFilePoints = localPointsProcessed;
                        state.SecondaryPercentrage = lastProgress;
                        worker.ReportProgress(0, state);
                    }

                    if(pointsProcessed == nextProcessCheck)
                    {
                        if(worker.CancellationPending)
                        {
                            octree.WaitUntillProcessed();
                            e.Cancel = true;
                            return;
                        }

                        nextProcessCheck += 10000000;
                        octree.WaitUntillProcessed();
                        state.Stage = ConversionStage.FlushingOctree;
                        worker.ReportProgress(0, state);
                        octree.Flush();
                        state.Stage = ConversionStage.BuildingOctree;
                    }
                }
            }

            //Process the files
            foreach(var source in sources)
            {
                localPointsProcessed = 0;
                numPoints = pointsList[sources.IndexOf(source)];

                state.Stage = ConversionStage.BuildingOctree;
                state.PrimaryPercentage = (int)(100 * (((double)sources.IndexOf(source) + 1) / ((double)sources.Count + 1)));
                state.SecondaryPercentrage = 0;
                state.SecondaryMessage = Path.GetFileNameWithoutExtension(source);
                state.TotalPoints = totalPoints;
                state.CurrentPoints = pointsProcessed;
                state.TotalFilePoints = currentOctree.NumAccepted;
                state.currentFilePoints = 0;
                worker.ReportProgress(0, state);

                PointReader reader = null;

                if (sourceFormats != null && sourceFormats.Count >= 0)
                    reader = CreatePointReader(source, sourceFormats[sources.IndexOf(source)]);
                else
                    reader = CreatePointReader(source);

                fraction = 1.0D / (double)numPoints;
                var lastProgress = 0;

                CloudPoint p = null;

                while(reader.ReadNextPoint(ref p))
                {
                    localPointsProcessed++;
                    pointsProcessed++;

                    octree.Add(p);
                    if((int)(localPointsProcessed * fraction * 100) > lastProgress)
                    {
                        if (worker.CancellationPending)
                        {
                            octree.WaitUntillProcessed();
                            reader.Close();
                            reader = null;
                            e.Cancel = true;
                            return;
                        }

                        lastProgress = (int)(localPointsProcessed * fraction * 100);
                        state.CurrentPoints = pointsProcessed;
                        state.currentFilePoints = localPointsProcessed;
                        state.SecondaryPercentrage = lastProgress;
                        worker.ReportProgress(0, state);
                    }

                    if(pointsProcessed == nextProcessCheck)
                    {
                        if (worker.CancellationPending)
                        {
                            octree.WaitUntillProcessed();
                            reader.Close();
                            reader = null;
                            e.Cancel = true;
                            return;
                        }

                        nextProcessCheck += 10000000;
                        octree.WaitUntillProcessed();
                        state.Stage = ConversionStage.FlushingOctree;
                        worker.ReportProgress(0, state);
                        octree.Flush();
                        state.Stage = ConversionStage.BuildingOctree;
                    }
                }

                reader.Close();
                reader = null;
            }

            state.Stage = ConversionStage.FinalizingOctree;
            state.CurrentPoints = pointsProcessed;
            worker.ReportProgress(0, state);
            octree.WaitUntillProcessed();
            octree.Flush();
        }

        private PointReader CreatePointReader(string source, AsciiFormat format = null)
        {
            PointReader reader = null;
            var upperSource = source.ToUpper();

            if (upperSource.EndsWith(".LAS") || upperSource.EndsWith(".LAZ"))
                reader = new LASPointReader(source);
            else if(upperSource.EndsWith(".XYZ") || upperSource.EndsWith(".TXT") || upperSource.EndsWith(".PTX") || upperSource.EndsWith(".PTY") || upperSource.EndsWith(".CSV") || upperSource.EndsWith(".GPF"))
            {
                if(format != null)
                {
                    reader = new AsciiReader(source, format, colorRange, intensityRange);
                }
            }
            else if(upperSource.EndsWith(".PTS"))
            {
                if(format != null)
                {
                    intensityRange = new List<double>();
                    intensityRange.Add(-2048);
                    intensityRange.Add(+2047);
                    reader = new AsciiReader(source, format, colorRange, intensityRange);
                }
            }
            else if(upperSource.EndsWith(".BIN"))
            {
                reader = new BinPointReader(source);
            }
            return reader;
        }

        private void Prepare()
        {
            //If sources contain directories use files inside the directory instead
            var sourceFiles = new List<string>();
            foreach(var source in sources)
            {
                if(Directory.Exists(source))
                {
                    //Its a directory
                    foreach(var file in Directory.GetFiles(source))
                    {
                        //Its a file
                        var upperFile = file.ToUpper();
                        if (upperFile.EndsWith(".LAS") || upperFile.EndsWith(".LAZ") || upperFile.EndsWith(".XYZ") || upperFile.EndsWith(".PTX") || upperFile.EndsWith(".TXT") || upperFile.EndsWith(".CSV") || upperFile.EndsWith(".GPF") || upperFile.EndsWith(".PTS"))
                            sourceFiles.Add(file);
                    }
                }
                else
                {
                    //Its a file
                    var upperFile = source.ToUpper();
                    if(upperFile.EndsWith(".LAS") || upperFile.EndsWith(".LAZ") || upperFile.EndsWith(".XYZ") || upperFile.EndsWith(".PTX") || upperFile.EndsWith(".TXT") || upperFile.EndsWith(".CSV") || upperFile.EndsWith(".GPF") || upperFile.EndsWith(".PTS") || upperFile.EndsWith(".BIN"))
                        sourceFiles.Add(source);
                }
            }

            this.sources = sourceFiles;
        }

        private Bounds CalculateBounds(BackgroundWorker worker, ref DoWorkEventArgs e)
        {
            Bounds newBounds = new Bounds();
            foreach(var source in sources)
            {
                if(worker.CancellationPending)
                {
                    e.Cancel = true;
                    return newBounds;
                }

                PointReader reader = null;
                if (sourceFormats != null && sourceFormats.Count >= 0)
                    reader = CreatePointReader(source, sourceFormats[sources.IndexOf(source)]);
                else
                    reader = CreatePointReader(source);

                Bounds lBounds = reader.GetBounds();
                pointsList.Add(reader.NumPoints());
                newBounds.Update(lBounds);

                reader.Close();
                reader = null;
            }

            return newBounds;
        }

        #endregion
    }

    public class Octree : Datasource
    {
        #region Properties

        public long NumPoints;
        public Bounds Bounds = new Bounds();
        public Bounds TightBounds = new Bounds();
        public Bounds OffsetBounds = new Bounds();
        public string WorkDir;
        public double Spacing;
        public double Scale;
        public int MaxDepth = -1;
        public OctreeNode Root;
        public ulong NumAdded = 0;
        public ulong NumAccepted = 0;
        public int HierachyStepSize = 5;
        public List<CloudPoint> Store = new List<CloudPoint>(1000000);
        public List<OctreeNode> LoadedNodes = new List<OctreeNode>();
        public List<OctreeNode> SelectedLoadedNodes = new List<OctreeNode>();
        public Thread StoreThread;
        public int PointsInMemory = 0;
        public OctreeFileVersion Version;
        public IntensityConverter IntensityConverter;
        public bool PointsDeleted = false;
        public List<OctreeNode> ModifiedNodes = new List<OctreeNode>();
        public Dictionary<object, List<OctreeNode>> SceneNodes = new Dictionary<object, List<OctreeNode>>();
        public Dictionary<object, List<(OctreeNode Node, RenderItem RenderItem)>> SelectedSceneNodes = new Dictionary<object, List<(OctreeNode, RenderItem)>>();

        public string LocalWorkDir
        {
            get
            {
                string localPath = "";

                var projectDir = Path.GetDirectoryName(FilePath);
                localPath = WorkDir.Replace(projectDir, "");

                return localPath;
            }
        }

        #endregion

        #region Setup

        public Octree(string projectPath)
        {
            this.FilePath = projectPath;
            if (FilePath.ToUpper().EndsWith(".JS") || FilePath.ToUpper().EndsWith(".PC"))
                Version = OctreeFileVersion.FileVersion1;
            else
                Version = OctreeFileVersion.FileVersion2;
        }

        public Octree(string workDir, string projectName, string projectPath)
        {
            this.WorkDir = workDir;
            this.Name = projectName;
            this.FilePath = projectPath;

            if (FilePath.ToUpper().EndsWith(".JS") || FilePath.ToUpper().EndsWith(".PC"))
                Version = OctreeFileVersion.FileVersion1;
            else
                Version = OctreeFileVersion.FileVersion2;
        }

        public Octree(string workDir, string projectName, string projectPath, Bounds bounds, double spacing, int maxDepth, double scale)
        {
            this.WorkDir = workDir;
            this.Name = projectName;
            this.FilePath = projectPath;
            this.Bounds = bounds;
            this.Spacing = spacing;
            this.MaxDepth = maxDepth;
            this.Scale = scale;

            if (FilePath.ToUpper().EndsWith(".JS") || FilePath.ToUpper().EndsWith(".PC"))
                Version = OctreeFileVersion.FileVersion1;
            else
                Version = OctreeFileVersion.FileVersion2;

            if(this.Scale == 0)
            {
                if (bounds.Size.Length() > 1000000)
                    this.Scale = 0.01;
                else if (bounds.Size.Length() > 100000)
                    this.Scale = 0.001;
                else if (bounds.Size.Length() > 1)
                    this.Scale = 0.001;
                else
                    this.Scale = 0.0001;
            }

            var scaleFactor = 1 / this.Scale;

            OffsetBounds = new Bounds();
            OffsetBounds.Update(Math.Round(bounds.Min.X / scaleFactor) * scaleFactor, Math.Round(bounds.Min.Y / scaleFactor) * scaleFactor, 0);

            Root = new OctreeNode(this, bounds);
        }

        #endregion

        #region Methods

        public void Add(CloudPoint point)
        {
            if (NumAdded == 0)
            {
                var dataDir = WorkDir + "/data";
                var tempDir = WorkDir + "/temp";

                Directory.CreateDirectory(dataDir);
                Directory.CreateDirectory(tempDir);
            }

            Store.Add(point);
            NumAdded++;

            if (Store.Count >= 1000000)
                ProcessStore();
        }

        public void ProcessStore()
        {
            List<CloudPoint> st = Store;
            Store = new List<CloudPoint>();

            WaitUntillProcessed();
            StoreThread = new Thread(new ParameterizedThreadStart(Process));
            StoreThread.Start(st);
        }

        private void Process(object state)
        {
            List<CloudPoint> st = (List<CloudPoint>)state;
            foreach (var p in st)
            {
                if (Root.Add(p))
                {
                    PointsInMemory++;
                    NumAccepted++;
                }
            }
        }

        public void WaitUntillProcessed()
        {
            if (StoreThread != null)
            {
                if (StoreThread.ThreadState == ThreadState.Running)
                    StoreThread.Join();
            }
        }

        public void Flush()
        {
            ProcessStore();

            if (StoreThread != null)
            {
                if (StoreThread.ThreadState == ThreadState.Running)
                    StoreThread.Join();
            }

            //Get all nodes to Flush
            var nodesToFlush = new List<OctreeNode>();
            Root.GetAllNodes(ref nodesToFlush);

            Parallel.ForEach(nodesToFlush, new ParallelOptions() { MaxDegreeOfParallelism = 4 }, (node) =>
            {
                node.Flush();
            });

            GC.Collect();
            //Root.Flush();
            //TightBounds = Root.AcceptedBounds;

            //Save the document 

            SaveFile();
        }

        public void SaveFile()
        {
            //Save the tempory nodes (rename the files)
            if(ModifiedNodes != null && PointsDeleted)
            {
                foreach (var node in ModifiedNodes)
                    node.CloseTemporaryNode(true);

                ModifiedNodes = new List<OctreeNode>();
                PointsDeleted = false;
            }

            if (File.Exists(FilePath))
                File.Delete(FilePath);

            var writer = new BinaryWriter(File.Open(FilePath, FileMode.Create, FileAccess.ReadWrite));
            writer.Write("NRG Point Cloud File Version 2");

            writer.Write(LocalWorkDir);
            writer.Write(Name);
            writer.Write(NumAccepted);

            writer.Write(HierachyStepSize);
            writer.Write(Spacing);
            writer.Write(Scale);

            writer.Write(Bounds.Min.X);
            writer.Write(Bounds.Min.Y);
            writer.Write(Bounds.Min.Z);
            writer.Write(Bounds.Max.X);
            writer.Write(Bounds.Max.Y);
            writer.Write(Bounds.Max.Z);

            writer.Write(TightBounds.Min.X);
            writer.Write(TightBounds.Min.Y);
            writer.Write(TightBounds.Min.Z);
            writer.Write(TightBounds.Max.X);
            writer.Write(TightBounds.Max.Y);
            writer.Write(TightBounds.Max.Z);

            writer.Write(OffsetBounds.Min.X);
            writer.Write(OffsetBounds.Min.Y);
            writer.Write(OffsetBounds.Min.Z);

            SaveNodeInfo(Root, writer);
            writer.Close();
            writer = null;
        }

        public void LoadStateFromDisk()
        {
            switch (Version)
            {
                case OctreeFileVersion.FileVersion1:
                    LoadFileVersion1();
                    break;
                case OctreeFileVersion.FileVersion2:
                    LoadFileVersion2();
                    break;
            }
        }

        private void LoadFileVersion1()
        {
            //Project file
            var projectFile = ((Services.OldFileVersions.PointCloudFile)Helpers.ReadPointCloudFile(FilePath)).ToNewPointCloudFile();

            this.HierachyStepSize = projectFile.HierarchyStepSize;
            this.Spacing = projectFile.Spacing;
            this.Scale = projectFile.Scale;
            this.Bounds = projectFile.BoundingBox;
            this.TightBounds = projectFile.TightBoundingBox;
            this.NumAccepted = projectFile.NumAccepted;
            this.WorkDir = Path.GetDirectoryName(FilePath) + Path.GetFileNameWithoutExtension(FilePath);

            //Tree
            var hrcPaths = new List<string>();
            hrcPaths.Add(WorkDir + "/data/r/r.hrc");
            string rootDir = WorkDir + "/data/r/";
            foreach (var directory in Directory.GetDirectories(rootDir))
            {
                foreach (var file in Directory.GetFiles(directory))
                {
                    if (file.EndsWith(".hrc"))
                        hrcPaths.Add(file);
                }

                foreach (var dir in Directory.GetDirectories(directory))
                {
                    foreach (var file in Directory.GetFiles(dir))
                    {
                        if (file.EndsWith(".hrc"))
                            hrcPaths.Add(file);
                    }
                }
            }

            hrcPaths.Sort();
            hrcPaths.Reverse();

            OctreeNode root = new OctreeNode(this, projectFile.BoundingBox);
            foreach (var hrcPath in hrcPaths)
            {
                string hrcName = Path.GetFileNameWithoutExtension(hrcPath);
                OctreeNode hrcRoot = root.FindNode(hrcName);

                OctreeNode current = hrcRoot;
                current.AddedSinceLastFlush = false;
                current.IsInMemory = false;
                var nodes = new List<OctreeNode>();
                nodes.Add(hrcRoot);

                var fin = new BinaryReader(File.Open(hrcPath, FileMode.Open, FileAccess.Read, FileShare.Read));

                //To do complete
                var length = fin.BaseStream.Length;
                var bytes = fin.ReadBytes((int)length);
                for (int i = 0; 5 * i < bytes.Length; i++)
                {
                    current = nodes[i];
                    byte children = bytes[i * 5];
                    byte[] p = new byte[4] { bytes[(i * 5) + 1], bytes[(i * 5) + 2], bytes[(i * 5) + 3], bytes[(i * 5) + 4] };
                    uint ip = BitConverter.ToUInt32(p, 0);
                    uint numPoints = ip;

                    //W.A. 10/01/2019 there is something seriously wrong here, will investigate later, for now its a hack
                    if (current.NumAccepted < numPoints)
                        current.NumAccepted = numPoints;

                    if (children != 0)
                    {
                        current.Children = new OctreeNode[8];
                        for (int j = 0; j < 8; j++)
                        {
                            if ((children & (1 << j)) != 0)
                            {
                                Bounds cBounds = OctreeInterop.ChildBounds(current.Bounds, j);
                                OctreeNode child = new OctreeNode(this, j, cBounds, current.Level + 1);
                                child.Parent = current;
                                child.AddedSinceLastFlush = false;
                                child.IsInMemory = false;
                                current.Children[j] = child;
                                nodes.Add(child);
                            }
                        }
                    }
                }
            }
            this.Root = root;
            this.NumAdded = 1;
        }

        private void LoadFileVersion2()
        {
            var reader = new BinaryReader(File.Open(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read));

            var version = reader.ReadString();
            if (version != "NRG Point Cloud File Version 2")
            {
                //Invalid file
                throw new Exception("Invalid file format");
            }
            Name = Path.GetFileName(FilePath);
            //this.WorkDir = reader.ReadString();
            var dirString = reader.ReadString();
            if (Directory.Exists(dirString))
                this.WorkDir = dirString;
            else
                this.WorkDir = Path.GetDirectoryName(FilePath) + "\\" + Path.GetFileNameWithoutExtension(Name)/* dirString*/;

            reader.ReadString();
            //this.ProjectName = reader.ReadString();
            this.NumAccepted = reader.ReadUInt64();
            this.HierachyStepSize = reader.ReadInt32();
            this.Spacing = reader.ReadDouble();
            this.Scale = reader.ReadDouble();

            this.Bounds = new Bounds();
            this.Bounds.Update(reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble());
            this.Bounds.Update(reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble());

            this.TightBounds = new Bounds();
            this.TightBounds.Update(reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble());
            this.TightBounds.Update(reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble());

            this.OffsetBounds = new Bounds();
            this.OffsetBounds.Update(reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble());

            Root = new OctreeNode(this, Bounds);
            LoadNodeInfo(Root, reader);

            reader.Close();
            reader = null;
        }

        private void LoadNodeInfo(OctreeNode node, BinaryReader reader)
        {
            //Read number of points
            var p = reader.ReadUInt32();
            node.NumAccepted = p;


            //Read children
            var children = reader.ReadByte();
            if (children != 0)
            {
                node.Children = new OctreeNode[8];
                for (int j = 0; j < 8; j++)
                {
                    if ((children & (1 << j)) != 0)
                    {
                        Bounds cBounds = OctreeInterop.ChildBounds(node.Bounds, j);
                        OctreeNode child = new OctreeNode(this, j, cBounds, node.Level + 1);
                        child.Parent = node;
                        child.AddedSinceLastFlush = false;
                        child.IsInMemory = false;
                        node.Children[j] = child;
                        LoadNodeInfo(child, reader);
                    }
                }
            }
            else
                return;
        }

        private void SaveNodeInfo(OctreeNode node, BinaryWriter writer)
        {
            //Save number of points
            writer.Write(node.NumAccepted);

            //Write children
            byte children = 0;
            if (node.Children != null)
            {
                for (int j = 0; j < (int)node.Children.Length; j++)
                {
                    if (node.Children[j] != null)
                        children = (byte)(children | (1 << j));
                }
            }
            writer.Write(children);

            if (node.Children != null)
            {
                for (int i = 0; i < node.Children.Length; i++)
                {
                    if (node.Children[i] != null)
                        SaveNodeInfo(node.Children[i], writer);
                }
            }
        }

        #endregion
    }

    public class OctreeNode
    {
        #region Properties

        public int Index = -1;
        public Bounds Bounds;
        public int Level = 0;
        public uint NumAccepted = 0;
        public Dictionary<(int, int, int), CloudPoint> Grid;
        public OctreeNode Parent = null;
        public OctreeNode[] Children;
        public bool AddedSinceLastFlush = true;
        public bool AddCalledSinceLastFlush = false;
        public Octree Octree;
        public int StoreLimit = 20000;
        public List<CloudPoint> Store = new List<CloudPoint>();
        public bool IsInMemory = true;
        public bool IsRendered = false;
        public bool IsSelectedRendered = false;
        public double GridSpacing;
        public double GridHalfSpacing;
        public double GridSize;
        public double SpacingFactor;
        public float[] Vertices;
        public float[] Colors;
        public SharedRenderItem RenderItem { get; set; }
        private double spacing = -999;
        public double lastDistFromCam = 999999;
        public double minDist = double.PositiveInfinity;
        public int i = 0;

        #endregion

        #region Setup

        public OctreeNode(Octree octree, Bounds bounds)
        {
            this.Octree = octree;
            this.Bounds = bounds;
            this.SpacingFactor = 1 / (Octree.Spacing / Math.Pow(2.0, (float)Level));
            SetupGrid();
        }

        public OctreeNode(Octree octree, int index, Bounds bounds, int level)
        {
            this.Index = index;
            this.Bounds = bounds;
            this.Level = level;
            this.Octree = octree;
            this.SpacingFactor = 1 / (Octree.Spacing / Math.Pow(2.0, (float)Level));
            SetupGrid();
        }

        #endregion

        #region Methods

        public string Name
        {
            get
            {
                if (Parent == null)
                    return "r";
                else
                    return Parent.Name + Index.ToString();
            }
        }

        public string FilePath
        {
            get
            {
                if (Octree.Version == OctreeFileVersion.FileVersion1)
                {
                    if (Parent == null)
                        return "r";
                    else
                    {
                        var name = Name;
                        if (name.Length >= Octree.HierachyStepSize + 1)
                        {
                            var splitName = "";
                            for (int i = 0; i < Math.Floor((double)name.Length / (double)Octree.HierachyStepSize); i++)
                            {
                                int end = 0;

                                if ((i * Octree.HierachyStepSize) + 1 + Octree.HierachyStepSize > name.Length)
                                    end = name.Length - ((i * Octree.HierachyStepSize) + 1);
                                else
                                    end = Octree.HierachyStepSize;

                                var substring = name.Substring((i * Octree.HierachyStepSize) + 1, Octree.HierachyStepSize);
                                splitName += substring + "\\";
                            }
                            splitName += name;
                            return splitName;
                        }
                        else
                            return name;
                    }
                }
                else if (Octree.Version == OctreeFileVersion.FileVersion2)
                {
                    if (Parent == null)
                        return "r";
                    else
                    {
                        var name = Name;
                        if (name.Length >= Octree.HierachyStepSize + 1)
                        {
                            var splitName = "";
                            for(int i = 0; i < Math.Floor(((double)name.Length - 1) / (double)Octree.HierachyStepSize); i++)
                            {
                                int end = 0;

                                if ((i * Octree.HierachyStepSize) + 1 + Octree.HierachyStepSize > name.Length)
                                    end = name.Length - ((i * Octree.HierachyStepSize) + 1);
                                else
                                    end = Octree.HierachyStepSize;

                                var substring = name.Substring((i * Octree.HierachyStepSize) + 1, end);
                                splitName += substring + "\\";
                            }

                            //Get remainder of the name
                            var characterCount = Level % 5;
                            if (characterCount == 0)
                                splitName += "r";
                            else
                                splitName += name.Substring(name.Length - characterCount, characterCount);
                          
                            return splitName;
                        }
                        else
                            return name;
                    }
                }
                else
                    return null;
            }
        }

        public double Spacing
        {
            get
            {
                if(spacing == -999)
                {
                    spacing = Octree.Spacing / Math.Pow(2.0, Level);
                }

                return spacing;
            }
        }

        public string WorkDir
        {
            get
            {
                return Octree.WorkDir;
            }
        }

        public string HierarchyPath
        {
            get
            {
                string path = "r/";

                int hierarchyStepSize = Octree.HierachyStepSize;
                string indices = Name.Substring(1);

                int numParts = (int)Math.Floor((double)indices.Length / (float)hierarchyStepSize);
                for (int i = 0; i < numParts; i++)
                    path += indices.Substring(i * hierarchyStepSize, hierarchyStepSize) + "/";

                return path;
            }
        }

        public bool IsLeafNode
        {
            get
            {
                if (Children == null)
                    return true;
                else
                    return false;
            }
        }

        public bool IsInnerNode
        {
            get
            {
                if (Children.Length > 0)
                    return true;
                else
                    return false;
            }
        }

        public void LoadFromDisk()
        {
            var reader = new BinaryReader(File.Open(WorkDir + "/data/r/" + FilePath + ".bin", FileMode.Open, FileAccess.Read, FileShare.Read));
            var scale = Octree.Scale;
            var bounds = OffsetBounds;
            uint c = 0;
            uint totalPoints = (uint)(reader.BaseStream.Length / 18);

            //Seperating these is a minor optimization but consindering the size of the data we could save a few billion if statements
            //So its worth the extra code...
            if(IsLeafNode)
            {
                //Add to store
                while(c <totalPoints)
                {
                    c++;
                    try
                    {
                        var p = new CloudPoint();
                        p.X = ((reader.ReadInt32() * scale) + bounds.Min.X);
                        p.Y = ((reader.ReadInt32() * scale) + bounds.Min.Y);
                        p.Z = ((reader.ReadInt32() * scale) + bounds.Min.Z);

                        //Alpha value is stored but not used so we must take next 4 bytes not 3
                        byte[] rgba = reader.ReadBytes(4);
                        p.R = rgba[0];
                        p.G = rgba[1];
                        p.B = rgba[2];

                        p.Intensity = reader.ReadUInt16();

                        //Add direct to store since we already established this is a leaf node
                        Store.Add(p);
                    }
                    catch
                    {
                        //We can assume if it fails to read one point the data is likely corrupted
                        break;
                    }
                }
            }
            else
            {
                //Add to grid
                while(c < totalPoints)
                {
                    c++;
                    try
                    {
                        var p = new CloudPoint();
                        p.X = ((reader.ReadInt32() * scale) + bounds.Min.X);
                        p.Y = ((reader.ReadInt32() * scale) + bounds.Min.Y);
                        p.Z = ((reader.ReadInt32() * scale) + bounds.Min.Z);

                        //Alpha value is stored but not used so we must take next 4 bytes not 3
                        byte[] rgba = reader.ReadBytes(4);
                        p.R = rgba[0];
                        p.G = rgba[1];
                        p.B = rgba[2];

                        p.Intensity = reader.ReadUInt16();

                        //Add direct to grid since we already established this to not be a leaf node
                        AddWithoutCheck(p);
                    }
                    catch
                    {
                        //We can assume if it fails to read one point the data is likely corrupted
                        break;
                    }
                }
            }

            reader.Close();
            reader = null;
            IsInMemory = true;
        }

        public void WriteToDisk(List<CloudPoint> points, bool append, bool temp = false)
        {
            string filePath = WorkDir + "/data/" + "r/" + FilePath;

            if (temp)
                filePath += ".tmp";
            else
                filePath += ".bin";

            Directory.CreateDirectory(WorkDir + "/data/" + HierarchyPath);

            if (File.Exists(filePath))
                File.Delete(filePath);

            var byteArray = new byte[points.Count * 18];
            int offset = 0;
            var scale = 1 / Octree.Scale;
            var bounds = OffsetBounds;

            this.NumAccepted = (uint)points.Count;

            foreach (var p in points)
            {
                int flatX = (int)Math.Round(((p.X - bounds.Min.X) * scale));
                int flatY = (int)Math.Round(((p.Y - bounds.Min.Y) * scale));
                int flatZ = (int)Math.Round(((p.Z - bounds.Min.Z) * scale));

                //Write X
                byteArray[offset++] = (byte)flatX;
                byteArray[offset++] = (byte)(flatX >> 8);
                byteArray[offset++] = (byte)(flatX >> 16);
                byteArray[offset++] = (byte)(flatX >> 24);

                //Write Y
                byteArray[offset++] = (byte)flatY;
                byteArray[offset++] = (byte)(flatY >> 8);
                byteArray[offset++] = (byte)(flatY >> 16);
                byteArray[offset++] = (byte)(flatY >> 24);

                //Write Z
                byteArray[offset++] = (byte)flatZ;
                byteArray[offset++] = (byte)(flatZ >> 8);
                byteArray[offset++] = (byte)(flatZ >> 16);
                byteArray[offset++] = (byte)(flatZ >> 24);

                //Write RGBA
                byteArray[offset++] = p.R;
                byteArray[offset++] = p.G;
                byteArray[offset++] = p.B;
                byteArray[offset++] = 255;

                //Write Intensity
                byteArray[offset++] = (byte)p.Intensity;
                byteArray[offset++] = (byte)(p.Intensity >> 8);
            }

            //Convert points to byte array
            //Write the byte array instead (keeps creating the byte array of of the file access section)
            var writer = new BinaryWriter(File.Open(filePath, FileMode.Create));
            writer.Write(byteArray);

            writer.Close();
            writer = null;
        }

        public void Flush()
        {
            //Put write to disk logic inside of this method to improve performance? (Saves passing a list of numbers through and this is the only place write to disk is called
            try
            {
                if (IsLeafNode)
                {
                    if (AddCalledSinceLastFlush)
                        WriteToDisk(Store, false);
                    else if (IsInMemory)
                    {
                        Store.Clear();
                        Store = new List<CloudPoint>();
                        IsInMemory = false;
                    }
                }
                else
                {
                    if (AddCalledSinceLastFlush)
                    {
                        //Try not discarding points after adding to them? will cost resources to read the file over and over again
                        WriteToDisk(Grid.Select(p => p.Value).ToList(), true);
                        Grid.Clear();
                        Grid = new Dictionary<(int, int, int), CloudPoint>();
                        IsInMemory = false;
                    }
                    else if (IsInMemory)
                    {
                        Grid = new Dictionary<(int, int, int), CloudPoint>();
                        IsInMemory = false;
                    }
                }

                AddCalledSinceLastFlush = false;
                //if (Children != null)
                //{
                //    foreach (var child in Children)
                //    {
                //        if (child != null)
                //            child.Flush();
                //    }
                //}
            }
            catch
            {

            }
        }
       
        public List<CloudPoint> LoadIntoStore()
        {
            try
            {
                var reader = new BinaryReader(File.Open(GetFullPath(), FileMode.Open, FileAccess.Read, FileShare.Read));
                var scale = Octree.Scale;
                var bounds = OffsetBounds;
                uint c = 0;
                uint totalPoints = (uint)(reader.BaseStream.Length / 18);

                var pointsList = new List<CloudPoint>((int)this.NumAccepted);

                while (c < totalPoints)
                {
                    c++;
                    try
                    {
                        var p = new CloudPoint();
                        p.X = ((reader.ReadInt32() * scale) + bounds.Min.X);
                        p.Y = ((reader.ReadInt32() * scale) + bounds.Min.Y);
                        p.Z = ((reader.ReadInt32() * scale) + bounds.Min.Z);

                        //Alpha value is stored but not used so we must take next 4 bytes not 3
                        byte[] rgba = reader.ReadBytes(4);
                        p.R = rgba[0];
                        p.G = rgba[1];
                        p.B = rgba[2];

                        p.Intensity = reader.ReadUInt16();

                        pointsList.Add(p);
                    }
                    catch
                    {
                        //We can assume if it fails to read one point the data is likely corrupted
                        break;
                    }
                }

                reader.Close();
                reader = null;
                return pointsList;
            }
            catch
            {
                //File most likely does not exist or is incorrectly formatted so ignore it.
                return new List<CloudPoint>();
            }
        }

        public bool Add(CloudPoint point)
        {
            try
            {
                AddCalledSinceLastFlush = true;

                if (!IsInMemory)
                    LoadFromDisk();

                if (IsLeafNode)
                {
                    Store.Add(point);
                    Octree.TightBounds.Update(point.X, point.Y, point.Z);
                    NumAccepted++;

                    if (Store.Count >= StoreLimit)
                    {
                        NumAccepted = 0;
                        Split();
                    }

                    return true;
                }
                else
                {
                    //Check if dictionary with string is faster
                    var xI = (int)Math.Round((point.X - Bounds.Min.X) * SpacingFactor);
                    var yI = (int)Math.Round((point.Y - Bounds.Min.Y) * SpacingFactor);
                    var zI = (int)Math.Round((point.Z - Bounds.Min.Z) * SpacingFactor);

                    var key = (xI, yI, zI);
                    if(Grid.TryGetValue(key, out CloudPoint oldPoint))
                    {
                        //Check if the point is a duplicate or near enough
                        if (Math.Abs(oldPoint.X - point.X) <= 0.00001 && Math.Abs(oldPoint.Y - point.Y) <= 0.00001 && Math.Abs(oldPoint.Z - point.Z) <= 0.00001)
                            return false;

                        int childIndex = OctreeInterop.NodeIndex(Bounds, point);
                        if (childIndex >= 0)
                        {
                            if (IsLeafNode)
                                Children = new OctreeNode[8];

                            OctreeNode child = Children[childIndex];
                            if (child == null)
                                child = CreateChild(childIndex);

                            if (Grid.Keys.Count == 1)
                            {

                            }
                            return child.Add(point);
                        }
                        else
                            return false;
                    }
                    else
                    {
                        Grid.Add(key, point);
                        Octree.TightBounds.Update(point.X, point.Y, point.Z);
                        NumAccepted++;
                        return true;
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        private void Split()
        {
            Children = new OctreeNode[8];
            string filePath = WorkDir + "/data/r/" + FilePath + ".bin";

            if (File.Exists(filePath))
            {
                //WA 13.02.19
                //Best guess is anti virus is locking up newly generated files meaning we can't delete them right away.
                //This loop just prevents the app from crashing while we wait to delete the file
                bool deleted = false;
                while (deleted == false)
                {
                    try
                    {
                        File.Delete(filePath);
                        deleted = true;
                    }
                    catch
                    {

                    }
                }
            }

            foreach (var p in Store)
            {
                Add(p);
            }

            Store.Clear();
            Store = new List<CloudPoint>();
        }

        /// <summary>
        /// Gets the full file path of the <see cref="OctreeNode"/> including its possible tempory location.
        /// </summary>
        /// <returns>Returns a <see cref="string"/> containing the full file path of the <see cref="OctreeNode"/></returns>
        public string GetFullPath()
        {
            var filePath = WorkDir + "/data/r/" + FilePath;

            //Check to see if a tmp file exists
            if (File.Exists(filePath + ".tmp"))
            {
                //If we have deleted points used the tmp file
                if (Octree.PointsDeleted)
                    filePath += ".tmp";
                else
                {
                    //Try to delete the tmp file
                    try
                    {
                        File.Delete(filePath + ".tmp");
                    }
                    catch
                    {

                    }
                    finally
                    {
                        filePath += ".bin";
                    }
                }
            }
            else
                filePath += ".bin";

            return filePath;
        }

        /// <summary>
        /// Traverses all child nodes and sets added since last flush to false - legacy for file version 1
        /// </summary>
        /// <param name="startNode"></param>
        public void Traverse(OctreeNode startNode)
        {
            startNode.AddedSinceLastFlush = false;
            if (this.Children != null)
            {
                foreach (var child in Children)
                {
                    if (child != null)
                        child.Traverse(child);
                }
            }
        }

        public List<OctreeNode> GetHierarchy(int levels)
        {
            var hierarchy = new List<OctreeNode>();

            var stack = new List<OctreeNode>();

            stack.Add(this);
            while (stack.Count != 0)
            {
                OctreeNode node = stack.First();
                stack.RemoveAt(0);

                if (node.Level >= this.Level + levels)
                    break;

                hierarchy.Add(node);

                if (node.Children != null)
                {
                    foreach (var child in node.Children)
                    {
                        if (child != null)
                            stack.Add(child);
                    }
                }
            }

            return hierarchy;
        }

        public OctreeNode FindNode(string name)
        {
            string thisName = this.Name;

            if (name.Length == thisName.Length)
                return (name == thisName) ? this : null;
            else if (name.Length > thisName.Length)
            {
                //Convert sring to int for last character
                int childIndex = Convert.ToInt32(name.Substring(thisName.Length, 1));
                if (!IsLeafNode && Children[childIndex] != null)
                    return Children[childIndex].FindNode(name);
                else
                    return null;
            }
            else
                return null;
        }

        private OctreeNode CreateChild(int childIndex)
        {
            var cBounds = OctreeInterop.ChildBounds(Bounds, childIndex);
            OctreeNode child = new OctreeNode(Octree, childIndex, cBounds, Level + 1);
            child.Parent = this;
            Children[childIndex] = child;

            return child;
        }

        private void AddWithoutCheck(CloudPoint point)
        {
            try
            {
                var xI = (int)Math.Round((point.X - Bounds.Min.X) * SpacingFactor);
                var yI = (int)Math.Round((point.Y - Bounds.Min.Y) * SpacingFactor);
                var zI = (int)Math.Round((point.Z - Bounds.Min.Z) * SpacingFactor);

                Grid.Add((xI, yI, zI), point);
            }
            catch
            {
                //Duplicate point
            }
        }

        public Bounds OffsetBounds
        {
            get
            {
                if (Octree.Version == OctreeFileVersion.FileVersion1)
                    return this.Bounds;
                else
                    return Octree.OffsetBounds;
            }
        }

        private void SetupGrid()
        {
            GridSpacing = Spacing;
            GridHalfSpacing = GridSpacing / 2;
            GridSize = Math.Ceiling(Bounds.Size.MaxValue()).ToString().Length;
            this.Grid = new Dictionary<(int, int, int), CloudPoint>();
        }

        public void LoadVertexData(ColorType colorType, Bounds modelBounds, bool cancel, BackgroundWorker worker, Alignment alignment, ShapeControl shapeControl, System.Drawing.Color underBreakColor, System.Drawing.Color overBreakColor)
        {
            if (worker.CancellationPending)
                return;

            var path = GetFullPath();

            if(File.Exists(path))
            {
                //Load file in
                var flatBounds = OffsetBounds;
                var scale = Octree.Scale;

                var reader = new BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read));

                //Just do RGB if we can't do clearance
                if (alignment == null || shapeControl == null)
                {
                    LoadVertexData(ColorType.RGB, modelBounds, cancel, worker);
                    return;
                }

                //Read all bytes

                if (reader == null)
                    return;

                var bytes = reader.ReadBytes((int)reader.BaseStream.Length);
                reader.Close();

                //Now sort through bytes to get points

                var numberOfPoints = (uint)(bytes.Length / 18);
                uint numberOfReadPoints = 0;
                int byteIndex = 0, vertIndex = 0, colIndex = 0;

                var center = modelBounds.Center;
                var cX = center.X;
                var cY = center.Y;

                Vertices = new float[numberOfPoints * 3];
                Colors = new float[numberOfPoints * 3];
                float uR = underBreakColor.R / 255.0f, uG = underBreakColor.G / 255.0f, uB = underBreakColor.B / 255.0f;
                float oR = overBreakColor.R / 255.0f, oG = overBreakColor.G / 255.0f, oB = overBreakColor.B / 255.0f;

                while (numberOfReadPoints < numberOfPoints)
                {
                    var x = ((BitConverter.ToInt32(bytes, byteIndex) * scale) + flatBounds.Min.X);
                    Vertices[vertIndex++] = (float)(x - cX);
                    byteIndex += 4;
                    var y = ((BitConverter.ToInt32(bytes, byteIndex) * scale) + flatBounds.Min.Y);
                    Vertices[vertIndex++] = (float)(y - cY);
                    byteIndex += 4;
                    var z = ((BitConverter.ToInt32(bytes, byteIndex) * scale) + flatBounds.Min.Z);
                    Vertices[vertIndex++] = (float)z;
                    byteIndex += 4;

                    //Calc the offset based on shape

                    double dist = 0, offset = 0;
                    double closestOffset = double.MaxValue;

                    var outVec = new AlignmentVector5();
                    if (alignment.Horizontal.GetChainageAndOffset(new Point2D(x, y), ref outVec))
                    {
                        var grade = alignment.Vertical.GradeLevel(outVec.chainage, out double vAngle);
                        var shapeWithCant = shapeControl.GetShape(outVec.chainage, alignment, grade, alignment.GetCantAtChainage(outVec.chainage), vAngle, ShapeType.Design);

                        if (shapeWithCant != null)
                        {
                            //Find closest element to alignment
                            foreach (var element in shapeWithCant.Elements)
                            {
                                var elementDistance = element.Length;

                                if (element.Radius != 0)
                                    Trig.DistanceAndOffsetFromLine(element.StartX, element.StartY, element.EndX, element.EndY, element.Radius, outVec.offset, z, ref dist, ref offset);
                                else
                                {
                                    double tBrg = 0, tDist = 0;
                                    Trig.RPC(element.StartX, element.StartY, element.EndX, element.EndY, ref tBrg, ref tDist);
                                    Trig.DistanceAndOffsetFromLine(element.StartX, element.StartY, tBrg, outVec.offset, z, ref dist, ref offset);
                                }

                                if (dist >= 0 && dist <= elementDistance)
                                {
                                    if (Math.Abs(offset) < Math.Abs(closestOffset))
                                        closestOffset = offset;
                                }
                            }

                            //Second pass to check distance from tangent points
                            foreach (var element in shapeWithCant.Elements)
                            {
                                var distToTangent = Vector.FnDistance(element.StartX, element.StartY, outVec.offset, z);
                                if (distToTangent < Math.Abs(closestOffset))
                                {
                                    if (outVec.offset < 0)
                                        closestOffset = outVec.offset <= element.StartX ? distToTangent * -1 : distToTangent;
                                    else
                                        closestOffset = outVec.offset >= element.StartX ? distToTangent * -1 : distToTangent;
                                }
                            }

                            //Now we have the closest 
                            if (closestOffset < 0 || closestOffset == double.MaxValue)
                            {
                                Colors[colIndex++] = oR;
                                Colors[colIndex++] = oG;
                                Colors[colIndex++] = oB;
                            }
                            else
                            {
                                Colors[colIndex++] = uR;
                                Colors[colIndex++] = uG;
                                Colors[colIndex++] = uB;
                            }
                        }
                        else
                        {
                            //Set color to black
                            Colors[colIndex++] = 0.0f;
                            Colors[colIndex++] = 0.0f;
                            Colors[colIndex++] = 0.0f;
                        }
                    }
                    else
                    {
                        //Set color to black
                        Colors[colIndex++] = 0.0f;
                        Colors[colIndex++] = 0.0f;
                        Colors[colIndex++] = 0.0f;
                    }

                    byteIndex += 6;
                    numberOfReadPoints++;
                }
            }
        }

        public void LoadVertexData(ColorType colorType, Bounds modelBounds, bool cancel, BackgroundWorker worker)
        {
            if (worker.CancellationPending)
                return;

            var path = GetFullPath();

            if(File.Exists(path))
            {
                //Load file in
                var flatBounds = OffsetBounds;
                var scale = Octree.Scale;

                var reader = new BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read));

                //Read all bytes
                if (reader == null)
                    return;

                var bytes = reader.ReadBytes((int)reader.BaseStream.Length);
                reader.Close();

                //Intensity stuff
                ushort intensity = 0;
                int minIntensity = Octree.IntensityConverter.MinIntensity, maxIntensity = Octree.IntensityConverter.MaxIntensity;
                int outOfRangeMode = Octree.IntensityConverter.OutOfRangeMode;
                var firstColor = Octree.IntensityConverter.IntensityColorList.First().StartColor;
                var lastColor = Octree.IntensityConverter.IntensityColorList.Last().EndColor;

                var center = modelBounds.Center;
                var cX = center.X;
                var cY = center.Y;

                //Now sort through bytes to get the points
                var numberOfPoints = (uint)(bytes.Length / 18);
                uint numberOfReadPoints = 0;
                int byteIndex = 0, vertIndex = 0, colIndex = 0;

                Vertices = new float[numberOfPoints * 3];
                Colors = new float[numberOfPoints * 3];
                float colScale = 1f / 255f;
                int cR = 0, cG = 0, cB = 0;

                switch (colorType)
                {
                    case ColorType.RGB:
                        while (numberOfReadPoints < numberOfPoints)
                        {
                            Vertices[vertIndex++] = (float)(((BitConverter.ToInt32(bytes, byteIndex) * scale) + flatBounds.Min.X) - cX);
                            byteIndex += 4;
                            Vertices[vertIndex++] = (float)(((BitConverter.ToInt32(bytes, byteIndex) * scale) + flatBounds.Min.Y) - cY);
                            byteIndex += 4;
                            Vertices[vertIndex++] = (float)(((BitConverter.ToInt32(bytes, byteIndex) * scale) + flatBounds.Min.Z));
                            byteIndex += 4;

                            Colors[colIndex++] = bytes[byteIndex++] * colScale;
                            Colors[colIndex++] = bytes[byteIndex++] * colScale;
                            Colors[colIndex++] = bytes[byteIndex++] * colScale;

                            byteIndex += 3;
                            numberOfReadPoints++;
                        }
                        return;
                    case ColorType.HeightMap:
                        while (numberOfReadPoints < numberOfPoints)
                        {
                            Vertices[vertIndex++] = (float)(((BitConverter.ToInt32(bytes, byteIndex) * scale) + flatBounds.Min.X) - cX);
                            byteIndex += 4;
                            Vertices[vertIndex++] = (float)(((BitConverter.ToInt32(bytes, byteIndex) * scale) + flatBounds.Min.Y) - cY);
                            byteIndex += 4;
                            var z = (float)(((BitConverter.ToInt32(bytes, byteIndex) * scale) + flatBounds.Min.Z));
                            Vertices[vertIndex++] = (float)z;
                            byteIndex += 4;

                            Services.Conversions.GetHeightMapFromPoint(modelBounds, z, ref cR, ref cG, ref cB);

                            Colors[colIndex++] = cR * colScale;
                            Colors[colIndex++] = cG * colScale;
                            Colors[colIndex++] = cB * colScale;

                            byteIndex += 6;
                            numberOfReadPoints++;
                        }
                        return;
                    case ColorType.GrayScale:
                        while (numberOfReadPoints < numberOfPoints)
                        {
                            Vertices[vertIndex++] = (float)(((BitConverter.ToInt32(bytes, byteIndex) * scale) + flatBounds.Min.X) - cX);
                            byteIndex += 4;
                            Vertices[vertIndex++] = (float)(((BitConverter.ToInt32(bytes, byteIndex) * scale) + flatBounds.Min.Y) - cY);
                            byteIndex += 4;
                            Vertices[vertIndex++] = (float)(((BitConverter.ToInt32(bytes, byteIndex) * scale) + flatBounds.Min.Z));
                            byteIndex += 4;

                            var gray = Services.Conversions.GetGrayScaleFromColor(bytes[byteIndex++], bytes[byteIndex++], bytes[byteIndex++]) * colScale;

                            Colors[colIndex++] = gray;
                            Colors[colIndex++] = gray;
                            Colors[colIndex++] = gray;

                            byteIndex += 3;
                            numberOfReadPoints++;
                        }
                        return;
                    case ColorType.Intensity:
                        var verts = new List<float>((int)numberOfPoints * 3);
                        var cols = new List<float>((int)numberOfPoints * 3);

                        while (numberOfReadPoints < numberOfPoints)
                        {
                            //Get intensity and check that it is within range
                            intensity = BitConverter.ToUInt16(bytes, byteIndex + 16);

                            if(intensity < minIntensity || intensity > maxIntensity)
                            {
                                //Intensity is out of range
                                if(outOfRangeMode == 0)
                                {
                                    //Show RGB
                                    verts.Add((float)(((BitConverter.ToInt32(bytes, byteIndex) * scale) + flatBounds.Min.X) - cX));
                                    byteIndex += 4;
                                    verts.Add((float)(((BitConverter.ToInt32(bytes, byteIndex) * scale) + flatBounds.Min.Y) - cY));
                                    byteIndex += 4;
                                    verts.Add((float)(((BitConverter.ToInt32(bytes, byteIndex) * scale) + flatBounds.Min.Z)));
                                    byteIndex += 4;

                                    cols.Add(bytes[byteIndex++] * colScale);
                                    cols.Add(bytes[byteIndex++] * colScale);
                                    cols.Add(bytes[byteIndex++] * colScale);

                                    byteIndex += 3;
                                    numberOfReadPoints++;
                                    continue;
                                }
                                else if(outOfRangeMode == 1)
                                {
                                    //Min / Max intensity
                                    verts.Add((float)(((BitConverter.ToInt32(bytes, byteIndex) * scale) + flatBounds.Min.X) - cX));
                                    byteIndex += 4;
                                    verts.Add((float)(((BitConverter.ToInt32(bytes, byteIndex) * scale) + flatBounds.Min.Y) - cY));
                                    byteIndex += 4;
                                    verts.Add((float)(((BitConverter.ToInt32(bytes, byteIndex) * scale) + flatBounds.Min.Z)));
                                    byteIndex += 10;

                                    if(intensity < minIntensity)
                                    {
                                        cols.Add(firstColor.R * colScale);
                                        cols.Add(firstColor.G * colScale);
                                        cols.Add(firstColor.B * colScale);
                                    }
                                    else if(intensity > maxIntensity)
                                    {
                                        cols.Add(lastColor.R * colScale);
                                        cols.Add(lastColor.G * colScale);
                                        cols.Add(lastColor.B * colScale);
                                    }

                                    numberOfReadPoints++;
                                    continue;
                                }
                                else if(outOfRangeMode == 2)
                                {
                                    //Hide the point so basically don't draw it
                                    byteIndex += 18;
                                    numberOfReadPoints++;
                                    continue;
                                }
                            }
                            else
                            {
                                Octree.IntensityConverter.GetIntensityColor(intensity, ref cR, ref cG, ref cB);

                                verts.Add((float)(((BitConverter.ToInt32(bytes, byteIndex) * scale) + flatBounds.Min.X) - cX));
                                byteIndex += 4;
                                verts.Add((float)(((BitConverter.ToInt32(bytes, byteIndex) * scale) + flatBounds.Min.Y) - cY));
                                byteIndex += 4;
                                verts.Add((float)(((BitConverter.ToInt32(bytes, byteIndex) * scale) + flatBounds.Min.Z)));
                                byteIndex += 10;

                                cols.Add(cR * colScale);
                                cols.Add(cG * colScale);
                                cols.Add(cB * colScale);

                                numberOfReadPoints++;
                                continue;
                            }
                        }

                        Vertices = verts.ToArray();
                        verts.Clear();

                        Colors = cols.ToArray();
                        cols.Clear();

                        return;
                }
            }
        }

        public void LoadVertexData(ColorType colorType, Bounds modelBounds, bool cancel, BackgroundWorker worker, List<ZonePolygon> selectionZones, List<ZonePolygon> deselectionZones)
        {
            if (worker.CancellationPending)
                return;

            var polysToCheck = new List<ZonePolygon>();
            var deselectPolysToCheck = new List<ZonePolygon>();

            foreach(var poly in selectionZones)
            {
                if (Bounds.Intersects3D(poly.Bounds) && !poly.Filtered)
                    polysToCheck.Add(poly);
            }

            if (polysToCheck.Count <= 0)
                return;

            foreach(var poly in deselectionZones)
            {
                if (Bounds.Intersects3D(poly.Bounds))
                    deselectPolysToCheck.Add(poly);
            }

            var path = GetFullPath();
            if (File.Exists(path))
            {
                //Load file in
                var flatBounds = OffsetBounds;
                var scale = Octree.Scale;

                var reader = new BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read));

                //Read all bytes
                if (reader == null)
                {
                    Vertices = null;
                    Colors = null;
                    return;
                }

                var bytes = reader.ReadBytes((int)reader.BaseStream.Length);
                reader.Close();

                //Now sort through bytes to get the points
                var numberOfPoints = (uint)(bytes.Length / 18);
                uint numberOfReadPoints = 0;
                int byteIndex = 0;
                bool load = true;

                var verts = new List<float>((int)numberOfPoints);
                var cols = new List<float>((int)numberOfPoints);

                var center = modelBounds.Center;
                var cX = center.X;
                var cY = center.Y;

                //Intensity stuff
                ushort intensity = 0;
                int minIntensity = Octree.IntensityConverter.MinIntensity, maxIntensity = Octree.IntensityConverter.MaxIntensity;
                int outOfRangeMode = Octree.IntensityConverter.OutOfRangeMode;
                var firstColor = Octree.IntensityConverter.IntensityColorList.First().StartColor;
                var lastColor = Octree.IntensityConverter.IntensityColorList.Last().EndColor;

                float colScale = 1f / 255f;
                int cR = 0, cG = 0, cB = 0;

                double x = 0, y = 0, z = 0;
                switch (colorType)
                {
                    case ColorType.RGB:
                        while (numberOfReadPoints < numberOfPoints)
                        {
                            load = false;

                            x = ((BitConverter.ToInt32(bytes, byteIndex) * scale) + flatBounds.Min.X);
                            byteIndex += 4;
                            y = ((BitConverter.ToInt32(bytes, byteIndex) * scale) + flatBounds.Min.Y);
                            byteIndex += 4;
                            z = ((BitConverter.ToInt32(bytes, byteIndex) * scale) + flatBounds.Min.Z);
                            byteIndex += 4;

                            foreach (var poly in selectionZones)
                            {
                                if (poly.Bounds.IsInside(x, y, z) && poly.InPoly2D(x, y, poly.Points))
                                {
                                    load = true;
                                    foreach (var deselectPoly in deselectionZones)
                                    {
                                        if (deselectPoly.Bounds.IsInside(x, y, z) && deselectPoly.InPoly2D(x, y, deselectPoly.Points))
                                        {
                                            load = false;
                                            break;
                                        }
                                    }

                                    break;
                                }
                            }

                            numberOfReadPoints++;

                            if (load)
                            {
                                verts.Add((float)(x - cX));
                                verts.Add((float)(y - cY));
                                verts.Add((float)z);

                                cols.Add(bytes[byteIndex++] * colScale);
                                cols.Add(bytes[byteIndex++] * colScale);
                                cols.Add(bytes[byteIndex++] * colScale);

                                byteIndex += 3;
                            }
                            else
                                byteIndex += 6;
                        }
                        break;
                    case ColorType.HeightMap:
                        while (numberOfReadPoints < numberOfPoints)
                        {
                            load = false;

                            x = ((BitConverter.ToInt32(bytes, byteIndex) * scale) + flatBounds.Min.X);
                            byteIndex += 4;
                            y = ((BitConverter.ToInt32(bytes, byteIndex) * scale) + flatBounds.Min.Y);
                            byteIndex += 4;
                            z = ((BitConverter.ToInt32(bytes, byteIndex) * scale) + flatBounds.Min.Z);
                            byteIndex += 4;

                            foreach (var poly in selectionZones)
                            {
                                if (poly.Bounds.IsInside(x, y, z) && poly.InPoly2D(x, y, poly.Points))
                                {
                                    load = true;

                                    foreach (var deselectPoly in deselectionZones)
                                    {
                                        if (deselectPoly.Bounds.IsInside(x, y, z) && deselectPoly.InPoly2D(x, y, deselectPoly.Points))
                                        {
                                            load = false;
                                            break;
                                        }
                                    }

                                    break;
                                }
                            }

                            numberOfReadPoints++;

                            if (load)
                            {
                                verts.Add((float)(x - cX));
                                verts.Add((float)(y - cY));
                                verts.Add((float)z);

                                Conversions.GetHeightMapFromPoint(modelBounds, z, ref cR, ref cG, ref cB);

                                cols.Add(cR * colScale);
                                cols.Add(cG * colScale);
                                cols.Add(cB * colScale);
                            }

                            byteIndex += 6;
                        }
                        break;
                    case ColorType.GrayScale:
                        while (numberOfReadPoints < numberOfPoints)
                        {
                            load = false;

                            x = ((BitConverter.ToInt32(bytes, byteIndex) * scale) + flatBounds.Min.X);
                            byteIndex += 4;
                            y = ((BitConverter.ToInt32(bytes, byteIndex) * scale) + flatBounds.Min.Y);
                            byteIndex += 4;
                            z = ((BitConverter.ToInt32(bytes, byteIndex) * scale) + flatBounds.Min.Z);
                            byteIndex += 4;

                            foreach (var poly in selectionZones)
                            {
                                if(poly.Bounds.IsInside(x, y, z) && poly.InPoly2D(x, y, poly.Points))
                                {
                                    load = true;
                                    foreach (var deselectPoly in deselectionZones)
                                    {
                                        if(deselectPoly.Bounds.IsInside(x, y, z) && deselectPoly.InPoly2D(x, y, deselectPoly.Points))
                                        {
                                            load = false;
                                            break;
                                        }
                                    }

                                    break;
                                }
                            }

                            numberOfReadPoints++;

                            if (load)
                            {
                                verts.Add((float)(x - cX));
                                verts.Add((float)(y - cY));
                                verts.Add((float)z);

                                var gray = Services.Conversions.GetGrayScaleFromColor(bytes[byteIndex++], bytes[byteIndex++], bytes[byteIndex++]) * colScale;

                                cols.Add(gray);
                                cols.Add(gray);
                                cols.Add(gray);
                                byteIndex += 3;
                            }
                            else
                                byteIndex += 6;
                        }
                        break;
                    case ColorType.Intensity:
                        while (numberOfReadPoints < numberOfPoints)
                        {
                            load = false;

                            x = ((BitConverter.ToInt32(bytes, byteIndex) * scale) + flatBounds.Min.X);
                            byteIndex += 4;
                            y = ((BitConverter.ToInt32(bytes, byteIndex) * scale) + flatBounds.Min.Y);
                            byteIndex += 4;
                            z = ((BitConverter.ToInt32(bytes, byteIndex) * scale) + flatBounds.Min.Z);
                            byteIndex += 4;

                            foreach (var poly in selectionZones)
                            {
                                if (poly.Bounds.IsInside(x, y, z) && poly.InPoly2D(x, y, poly.Points))
                                {
                                    load = true;
                                    foreach (var deselectPoly in deselectionZones)
                                    {
                                        if (deselectPoly.Bounds.IsInside(x, y, z) && deselectPoly.InPoly2D(x, y, deselectPoly.Points))
                                        {
                                            load = false;
                                            break;
                                        }
                                    }

                                    break;
                                }
                            }

                            numberOfReadPoints++;

                            if (load)
                            {
                                //X, Y, Z already loaded at this point
                                intensity = BitConverter.ToUInt16(bytes, byteIndex + 4);
                                
                                if(intensity < minIntensity || intensity > maxIntensity)
                                {
                                    //Intensity is out of range
                                    if(outOfRangeMode == 0)
                                    {
                                        //Show RGB
                                        verts.Add((float)(x - cX));
                                        verts.Add((float)(y - cY));
                                        verts.Add((float)z);

                                        cols.Add(bytes[byteIndex++] * colScale);
                                        cols.Add(bytes[byteIndex++] * colScale);
                                        cols.Add(bytes[byteIndex++] * colScale);

                                        byteIndex += 3;
                                        continue;
                                    }
                                    else if(outOfRangeMode == 1)
                                    {
                                        //Min / Max Intensity
                                        verts.Add((float)(x - cX));
                                        verts.Add((float)(y - cY));
                                        verts.Add((float)z);
                                        byteIndex += 6;

                                        if(intensity < minIntensity)
                                        {
                                            cols.Add(firstColor.R * colScale);
                                            cols.Add(firstColor.G * colScale);
                                            cols.Add(firstColor.B * colScale);
                                        }
                                        else if(intensity > maxIntensity)
                                        {
                                            cols.Add(lastColor.R * colScale);
                                            cols.Add(lastColor.G * colScale);
                                            cols.Add(lastColor.B * colScale);
                                        }

                                        continue;
                                    }
                                    else if(outOfRangeMode == 2)
                                    {
                                        //Hide the point
                                        byteIndex += 6;
                                        continue;
                                    }
                                }
                                else
                                {
                                    //Point is within the intensity range so draw it normally
                                    Octree.IntensityConverter.GetIntensityColor(intensity, ref cR, ref cG, ref cB);

                                    verts.Add((float)(x - cX));
                                    verts.Add((float)(y - cY));
                                    verts.Add((float)z);
                                    byteIndex += 6;

                                    cols.Add(cR * colScale);
                                    cols.Add(cG * colScale);
                                    cols.Add(cB * colScale);
                                    continue;
                                }
                            }
                            else
                                byteIndex += 6;
                        }
                        break;
                }

                //If there is data convert to float arrays and return
                if (verts.Count > 0 && cols.Count > 0)
                {
                    Vertices = verts.ToArray();
                    Colors = cols.ToArray();
                }
                else
                {
                    Vertices = null;
                    Colors = null;
                    verts.Clear();
                    cols.Clear();
                    return;
                }
            }
        }

        public void  GetAllNodes(ref List<OctreeNode> nodes)
        {
            nodes.Add(this);

            if(Children != null)
            {
                foreach(var child in Children)
                {
                    if (child != null)
                        child.GetAllNodes(ref nodes);
                }
            }
        }

        public void GetAllNodesToFlush(ref List<OctreeNode> nodes)
        {
            if (this.IsInMemory)
                nodes.Add(this);

            if(Children != null)
            {
                foreach(var child in Children)
                {
                    if (child != null)
                        child.GetAllNodesToFlush(ref nodes);
                }
            }
        }

        /// <summary>
        /// Generates a List of <see cref="OctreeNode"/> objects that intersect with the provided <see cref="Bounds"/> object.
        /// </summary>
        /// <param name="boundToCheck">A <see cref="Bounds"/> object to check against the <see cref="OctreeNode.Bounds"/></param>
        /// <param name="nodes">A List of <see cref="OctreeNode"/> object that intersect with the <see cref="OctreeNode.Bounds"/></param>
        public void GetNodesForBounds(Bounds boundToCheck, ref List<OctreeNode> nodes)
        {
            if (Bounds.Intersects3D(boundToCheck))
            {
                nodes.Add(this);

                if(Children != null)
                {
                    foreach(var child in Children)
                    {
                        child?.GetNodesForBounds(boundToCheck, ref nodes);
                    }
                }
            }
        }

        public bool DeletePoints(ClippingBox deleteArea)
        {
            try
            {
                bool pointsDeleted = false;

                //Get the points 
                var points = LoadIntoStore();
                var numPoints = points.Count;

                for (int i = 0; i < points.Count;)
                {
                    //If the point is in the delete area then remove it
                    if (deleteArea.IsInside3D(points[i]))
                        points.RemoveAt(i);
                    else
                        i++;
                }

                //If the number of points has reduced
                if (points.Count < numPoints)
                    pointsDeleted = true;

                if (pointsDeleted)
                {
                    //Save the new file as a temp file
                    WriteToDisk(points, false, true);
                    Octree.PointsDeleted = true;

                    if (!Octree.ModifiedNodes.Contains(this))
                        Octree.ModifiedNodes.Add(this);

                    NumAccepted = (uint)points.Count;
                    Octree.NumAccepted -= (ulong)(numPoints - points.Count);
                }

                return pointsDeleted;
            }
            catch
            {
                return false;
            }
        }

        public void CloseTemporaryNode(bool saveNode)
        {
            try
            {
                var path = WorkDir + "/data/" + "r/" + FilePath;
                var tmpPath = path + ".tmp";
                var binPath = path + ".bin";

                if (saveNode)
                {
                    //Replace the current node with the temporary one
                    if (File.Exists(tmpPath))
                    {
                        if (File.Exists(binPath))
                            File.Delete(binPath);

                        File.Move(tmpPath, binPath);
                    }
                }
                else
                {
                    //Remove the temporary node as it is no longer needed
                    if (File.Exists(tmpPath))
                        File.Delete(tmpPath);
                }
            }
            catch(Exception e)
            {
                //Most likely file is in use
            }
        }

        #endregion

        #region Data Extraction

        /// <summary>
        /// Gets the closest point from the <see cref="OctreeNode"/> points
        /// </summary>
        /// <param name="ray">A <see cref="Ray"/> to test the distance of each point from</param>
        /// <param name="x">A <see cref="double"/> to store the X value of the closest point</param>
        /// <param name="y">A <see cref="double"/> to store the Y value of the closest point</param>
        /// <param name="z">A <see cref="double"/> to store the Z value of the closest point</param>
        /// <param name="r">A <see cref="byte"/> to store the Red color channel of the closest point</param>
        /// <param name="g">A <see cref="byte"/> to store the Green color channel of the closest point</param>
        /// <param name="b">A <see cref="byte"/> to store the Blue color channel of the closest point</param>
        /// <param name="intensity">A <see cref="ushort"/> to store the Intensity of the closest point</param>
        /// <param name="distance">A <see cref="double"/> representing the current closest distance to the <see cref="Ray"/></param>
        /// <returns>Returns true if the closest point has been found, otherwise returns false</returns>
        public bool GetClosest(Ray ray, ref double x, ref double y, ref double z, ref byte r, ref byte g, ref byte b, ref ushort intensity, ref double distance)
        {
            if (Bounds.IntersectsWithRay(ray))
            {
                var points = LoadIntoStore();
                foreach (var p in points)
                {
                    var newDist = Vector.ClosestDistanceOnALine(ray.Origin.X, ray.Origin.Y, ray.Origin.Z, ray.End.X, ray.End.Y, ray.End.Z, p.X, p.Y, p.Z);
                    if (newDist < distance)
                    {
                        x = p.X;
                        y = p.Y;
                        z = p.Z;
                        r = p.R;
                        g = p.G;
                        b = p.B;
                        intensity = p.Intensity;
                        distance = newDist;
                    }
                }
                points.Clear();
                //points = new List<Point>();
                //grid = null;


                if (Children != null)
                {
                    foreach (var child in Children)
                    {
                        if (child != null)
                        {
                            child.GetClosest(ray, ref x, ref y, ref z, ref r, ref g, ref b, ref intensity, ref distance);
                        }
                    }
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the closest point in relation to the camera from the <see cref="OctreeNode"/> points
        /// </summary>
        /// <param name="ray">A <see cref="Ray"/> to test the distance of each point from</param>
        /// <param name="x">A <see cref="double"/> to store the X value of the closest point</param>
        /// <param name="y">A <see cref="double"/> to store the Y value of the closest point</param>
        /// <param name="z">A <see cref="double"/> to store the Z value of the closest point</param>
        /// <param name="r">A <see cref="byte"/> to store the Red color channel of the closest point</param>
        /// <param name="g">A <see cref="byte"/> to store the Green color channel of the closest point</param>
        /// <param name="b">A <see cref="byte"/> to store the Blue color channel of the closest point</param>
        /// <param name="intensity">A <see cref="ushort"/> to store the Intensity of the closest point</param>
        /// <param name="distance">A <see cref="double"/> representing the current closest distance to the <see cref="Ray"/></param>
        /// <returns>Returns true if the closest point has been found, otherwise returns false</returns>
        public bool GetClosestInOffsetRange(Ray ray, ref double x, ref double y, ref double z, ref byte r, ref byte g, ref byte b, ref ushort intensity, ref double distance, double offSetMaxRange, List<CloudPoint> pointsInOffset)
        {
            if (Bounds.IntersectsWithRay(ray))
            {
             
                var points = LoadIntoStore();
                foreach (var p in points)
                {
                    //var newDistFromCam = NRG.MathsHelpers.Vector.Fn3DDistance(ray.Origin.X, ray.Origin.Y, ray.Origin.Z, p.X, p.Y, p.Z);
                    var newDist = Vector.ClosestDistanceOnALine(ray.Origin.X, ray.Origin.Y, ray.Origin.Z, ray.End.X, ray.End.Y, ray.End.Z, p.X, p.Y, p.Z);

                    if (newDist < offSetMaxRange)
                    {
                        x = p.X;
                        y = p.Y;
                        z = p.Z;
                        r = p.R;
                        g = p.G;
                        b = p.B;
                        intensity = p.Intensity;
                        distance = newDist;
                        //lastDistFromCam = newDistFromCam;
                        pointsInOffset.Add(new CloudPoint(x,y,z,r,g,b,intensity));
                    }
                }
                points.Clear();
                //points = new List<Point>();
                //grid = null;


                if (Children != null)
                {
                    foreach (var child in Children)
                    {
                        if (child != null)
                        {
                            child.GetClosestInOffsetRange(ray, ref x, ref y, ref z, ref r, ref g, ref b, ref intensity, ref distance, offSetMaxRange, pointsInOffset);
                        }
                    }
                }

                return true;
            }
            else
            {
                return false;
            }
        }


        /// <summary>
        /// Gets the closest point from the <see cref="OctreeNode"/> points
        /// </summary>
        /// <param name="ray">A <see cref="Ray"/> to test the distance of each point from</param>
        /// <param name="x">A <see cref="double"/> to store the X value of the closest point</param>
        /// <param name="y">A <see cref="double"/> to store the Y value of the closest point</param>
        /// <param name="z">A <see cref="double"/> to store the Z value of the closest point</param>
        /// <param name="r">A <see cref="byte"/> to store the Red color channel of the closest point</param>
        /// <param name="g">A <see cref="byte"/> to store the Green color channel of the closest point</param>
        /// <param name="b">A <see cref="byte"/> to store the Blue color channel of the closest point</param>
        /// <param name="intensity">A <see cref="ushort"/> to store the Intensity of the closest point</param>
        /// <param name="distance">A <see cref="double"/> representing the current closest distance to the <see cref="Ray"/></param>
        /// <param name="worker">The <see cref="BackgroundWorker"/> managing this process</param>
        /// <param name="e">The <see cref="DoWorkEventArgs"/> of the <see cref="BackgroundWorker"/> managing this process</param>
        /// <returns>Returns true if the closest point has been found, otherwise returns false</returns>
        public bool GetClosest(Ray ray, ref double x, ref double y, ref double z, ref byte r, ref byte g, ref byte b, ref ushort intensity, ref double distance, ref BackgroundWorker worker, DoWorkEventArgs e)
        {
            if (worker.CancellationPending)
                return false;

            if (Bounds.IntersectsWithRay(ray))
            {
                if (worker.CancellationPending)
                    return false;

                var points = LoadIntoStore();
                if (worker.CancellationPending)
                {
                    return false;
                }
                foreach (var p in points)
                {
                    var newDist = Vector.ClosestDistanceOnALine(ray.Origin.X, ray.Origin.Y, ray.Origin.Z, ray.End.X, ray.End.Y, ray.End.Z, p.X, p.Y, p.Z);
                    if (newDist < distance)
                    {
                        x = p.X;
                        y = p.Y;
                        z = p.Z;
                        r = p.R;
                        g = p.G;
                        b = p.B;
                        intensity = p.Intensity;
                        distance = newDist;
                    }
                }
                points.Clear();

                if (worker.CancellationPending)
                    return false;

                if (Children != null)
                {
                    foreach (var child in Children)
                    {
                        if (child != null)
                            child.GetClosest(ray, ref x, ref y, ref z, ref r, ref g, ref b, ref intensity, ref distance, ref worker, e);
                    }
                }
                return true;
            }
            else
                return false;
        }

        /// <summary>
        /// Gets the closest point from the currently selected points within the <see cref="OctreeNode"/>
        /// </summary>
        /// <param name="ray">A <see cref="Ray"/> to test the distance of each point from</param>
        /// <param name="selectionZones">A List of <see cref="Polygon"/> selection zones representing the points to test</param>
        /// <param name="deselectionZones">A List of <see cref="Polygon"/> deselection zones representing the points to skip</param>
        /// <param name="x">A <see cref="double"/> to store the X value of the closest point</param>
        /// <param name="y">A <see cref="double"/> to store the Y value of the closest point</param>
        /// <param name="z">A <see cref="double"/> to store the Z value of the closest point</param>
        /// <param name="r">A <see cref="byte"/> to store the Red color channel of the closest point</param>
        /// <param name="g">A <see cref="byte"/> to store the Green color channel of the closest point</param>
        /// <param name="b">A <see cref="byte"/> to store the Blue color channel of the closest point</param>
        /// <param name="intensity">A <see cref="ushort"/> to store the Intensity of the closest point</param>
        /// <param name="distance">A <see cref="double"/> representing the current closest distance to the <see cref="Ray"/></param>
        /// <returns>Returns true if the closest point has been found, otherwise returns false</returns>
        public bool GetClosestPointFromSelection(Ray ray, List<ZonePolygon> selectionZones, List<ZonePolygon> deselectionZones, ref double x, ref double y, ref double z, ref byte r, ref byte g, ref byte b, ref ushort intensity, ref double distance)
        {
            if (Bounds.IntersectsWithRay(ray))
            {
                bool cont = false;

                //Check if bounds intersects with any selectionzones
                var polysToCheck = new List<ZonePolygon>();
                var deselectPolysToCheck = new List<ZonePolygon>();

                foreach (var zone in selectionZones)
                {
                    if (Bounds.Intersects3D(zone.Bounds))
                        polysToCheck.Add(zone);
                }

                if (polysToCheck.Count <= 0)
                    return false;

                foreach (var zone in deselectionZones)
                {
                    if (Bounds.Intersects3D(zone.Bounds))
                        deselectPolysToCheck.Add(zone);
                }

                var points = LoadIntoStore();
                foreach (var p in points)
                {
                    cont = false;
                    //Check its selected
                    foreach (var poly in polysToCheck)
                    {
                        if (poly.Bounds.IsInside(p.X, p.Y, p.Z) && poly.InPoly2D(p.X, p.Y, poly.Points))
                        {
                            cont = true;

                            foreach(var deselectPoly in deselectPolysToCheck)
                            {
                                if (deselectPoly.Bounds.IsInside(p.X, p.Y, p.Z) && deselectPoly.InPoly2D(p.X, p.Y, deselectPoly.Points))
                                {
                                    cont = false;
                                    break;
                                }
                            }

                            break;
                        }
                    }

                    if (!cont)
                        continue;

                    //If we made it to here its both selected point and no deselected, get distance and check value
                    var newDist = Vector.ClosestDistanceOnALine(ray.Origin.X, ray.Origin.Y, ray.Origin.Z, ray.End.X, ray.End.Y, ray.End.Z, p.X, p.Y, p.Z);
                    if (newDist < distance)
                    {
                        x = p.X;
                        y = p.Y;
                        z = p.Z;
                        r = p.R;
                        g = p.G;
                        b = p.B;
                        intensity = p.Intensity;
                        distance = newDist;
                    }
                }
                points.Clear();

                if (Children != null)
                {
                    foreach (var child in Children)
                        if (child != null)
                            child.GetClosestPointFromSelection(ray, polysToCheck, deselectPolysToCheck, ref x, ref y, ref z, ref r, ref g, ref b, ref intensity, ref distance);
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the closest point from the currently selected points within the <see cref="OctreeNode"/>
        /// </summary>
        /// <param name="ray">A <see cref="Ray"/> to test the distance of each point from</param>
        /// <param name="selectionZones">A List of <see cref="Polygon"/> selection zones representing the points to test</param>
        /// <param name="deselectionZones">A List of <see cref="Polygon"/> deselection zones representing the points to skip</param>
        /// <param name="x">A <see cref="double"/> to store the X value of the closest point</param>
        /// <param name="y">A <see cref="double"/> to store the Y value of the closest point</param>
        /// <param name="z">A <see cref="double"/> to store the Z value of the closest point</param>
        /// <param name="r">A <see cref="byte"/> to store the Red color channel of the closest point</param>
        /// <param name="g">A <see cref="byte"/> to store the Green color channel of the closest point</param>
        /// <param name="b">A <see cref="byte"/> to store the Blue color channel of the closest point</param>
        /// <param name="intensity">A <see cref="ushort"/> to store the Intensity of the closest point</param>
        /// <param name="distance">A <see cref="double"/> representing the current closest distance to the <see cref="Ray"/></param>
        /// <param name="worker">The <see cref="BackgroundWorker"/> managing this process</param>
        /// <param name="e">The <see cref="DoWorkEventArgs"/> of the <see cref="BackgroundWorker"/> managing this process</param>
        /// <returns>Returns true if the closest point has been found, otherwise returns false</returns>
        public bool GetClosestPointFromSelection(Ray ray, List<ZonePolygon> selectionZones, List<ZonePolygon> deselectionZones, ref double x, ref double y, ref double z, ref byte r, ref byte g, ref byte b, ref ushort intensity, ref double distance, ref BackgroundWorker worker, DoWorkEventArgs e)
        {
            if (Bounds.IntersectsWithRay(ray))
            {
                if (worker.CancellationPending)
                {
                    return false;
                }

                bool cont = false;

                //Check if bounds intersections with any selectionZones
                var polysToCheck = new List<ZonePolygon>();
                var deselectPolysToCheck = new List<ZonePolygon>();

                foreach (var zone in selectionZones)
                {
                    if (Bounds.Intersects3D(zone.Bounds))
                        polysToCheck.Add(zone);
                }

                if (polysToCheck.Count <= 0)
                    return false;

                foreach (var zone in deselectionZones)
                {
                    if (Bounds.Intersects3D(zone.Bounds))
                        deselectPolysToCheck.Add(zone);
                }

                var points = LoadIntoStore();
                foreach (var p in points)
                {
                    cont = false;
                    //Check its selected
                    foreach (var zone in polysToCheck)
                    {
                        if (zone.Bounds.IsInside(p.X, p.Y, p.Z) && zone.InPoly2D(p.X, p.Y, zone.Points))
                        {
                            cont = true;

                            foreach(var deselectPoly in deselectPolysToCheck)
                            {
                                if(deselectPoly.Bounds.IsInside(p.X, p.Y, p.Z) && deselectPoly.InPoly2D(p.X, p.Y, deselectPoly.Points))
                                {
                                    cont = false;
                                    break;
                                }
                            }

                            break;
                        }
                    }

                    if (!cont)
                        continue;

                    //If we made it to here its both selected and not deselected, get distance and check value
                    var newDist = Vector.ClosestDistanceOnALine(ray.Origin.X, ray.Origin.Y, ray.Origin.Z, ray.End.X, ray.End.Y, ray.End.Z, p.X, p.Y, p.Z);
                    if (newDist < distance)
                    {
                        x = p.X;
                        y = p.Y;
                        z = p.Z;
                        r = p.R;
                        g = p.G;
                        b = p.B;
                        intensity = p.Intensity;
                        distance = newDist;
                    }
                }
                points.Clear();

                if (Children != null)
                {
                    foreach (var child in Children)
                        if (child != null)
                            child.GetClosestPointFromSelection(ray, polysToCheck, deselectPolysToCheck, ref x, ref y, ref z, ref r, ref g, ref b, ref intensity, ref distance, ref worker, e);
                }
                return true;
            }
            else
                return false;
        }

        /// <summary>
        /// Gathers selected points within the cross section and returns them in addition to gathering the closest point
        /// </summary>
        /// <param name="ray">The <see cref="Ray"/> through the model towards the closest point</param>
        /// <param name="section">The <see cref="CrossSection"/> object to save points to</param>
        /// <param name="selectionZones">A List of <see cref="Polygon"/> selection zones to take points from</param>
        /// <param name="deselectionZones">A List<see cref="Polygon"/> deselection zones to ignore points from</param>
        /// <param name="distance">The distance to the current closest point</param>
        /// <param name="worker">The <see cref="BackgroundWorker"/> handling the cross section generation</param>
        /// <param name="e">The <see cref="DoWorkEventArgs"/> to check whether the worker can been cancelled</param>
        /// <returns>Returns true if some cross section points are found</returns>
        public bool GetSectionFromSelectionWithClosest(Ray ray, ref CrossSection section, List<ZonePolygon> selectionZones, List<ZonePolygon> deselectionZones, ref double distance, ref BackgroundWorker worker, DoWorkEventArgs e)
        {
            //Get all points within section bounds
            if (Bounds.Intersects3D(section.ClippingBox.Bounds))
            {
                if (worker.CancellationPending == true)
                {
                    e.Cancel = true;
                    return false;
                }

                //Generate a list of zones to check
                var polysToCheck = new List<ZonePolygon>();
                var deselectPolysToCheck = new List<ZonePolygon>();

                foreach (var zone in selectionZones)
                {
                    if (Bounds.Intersects2D(zone.Bounds))
                        polysToCheck.Add(zone);
                }

                if (polysToCheck.Count <= 0)
                    return false;

                foreach (var zone in deselectionZones)
                {
                    if (Bounds.Intersects2D(zone.Bounds))
                        deselectPolysToCheck.Add(zone);
                }

                //Get all points within the section
                var points = LoadIntoStore();
                bool cont;

                if (Bounds.IntersectsWithRay(ray))
                {
                    foreach (var p in points)
                    {
                        if (section.ClippingBox.IsInside3D(p.X, p.Y, p.Z))
                        {
                            cont = false;
                            //Check its selected
                            foreach (var zone in polysToCheck)
                            {
                                if (zone.Bounds.IsInside(p.X, p.Y, p.Z) && zone.InPoly2D(p.X, p.Y, zone.Points))
                                {
                                    cont = true;

                                    foreach(var deselectPoly in deselectPolysToCheck)
                                    {
                                        if(deselectPoly.Bounds.IsInside(p.X, p.Y, p.Z) && deselectPoly.InPoly2D(p.X, p.Y, deselectPoly.Points))
                                        {
                                            cont = false;
                                            break;
                                        }
                                    }

                                    break;
                                }
                            }

                            if (!cont)
                                continue;

                            //Point is selected and not deselected
                            var newDist = Vector.ClosestDistanceOnALine(ray.Origin.X, ray.Origin.Y, ray.Origin.Z, ray.End.X, ray.End.Y, ray.End.Z, p.X, p.Y, p.Z);
                            if (newDist < distance)
                            {
                                section.ClosestPoint = new CloudPoint(p.X, p.Y, p.Z, p.R, p.G, p.B);
                                distance = newDist;
                            }

                            double newX = 0, newY = 0;
                            Trig.DistanceAndOffsetFromLine(section.SectionCenter.X, section.SectionCenter.Y, section.Bearing, p.X, p.Y, ref newX, ref newY);
                            section.SectionBounds.Update(newX, p.Z, newY);
                            section.OriginalPoints.Add(new SectionPoint(newX, p.Z, newY, p.R, p.G, p.B));
                        }
                    }
                }
                else
                {
                    foreach (var p in points)
                    {
                        if (section.ClippingBox.IsInside3D(p.X, p.Y, p.Z))
                        {
                            cont = false;
                            //Check its selected
                            foreach (var zone in polysToCheck)
                            {
                                if (zone.Bounds.IsInside(p.X, p.Y, p.Z) && zone.InPoly2D(p.X, p.Y, zone.Points))
                                {
                                    cont = true;

                                    foreach(var deselectPoly in deselectPolysToCheck)
                                    {
                                        if(deselectPoly.Bounds.IsInside(p.X, p.Y, p.Z) && deselectPoly.InPoly2D(p.X, p.Y, deselectPoly.Points))
                                        {
                                            cont = false;
                                            break;
                                        }
                                    }

                                    break;
                                }
                            }

                            if (!cont)
                                continue;

                            //Point is selected and not deselected
                            double newX = 0, newY = 0;
                            Trig.DistanceAndOffsetFromLine(section.SectionCenter.X, section.SectionCenter.Y, section.Bearing, p.X, p.Y, ref newX, ref newY);
                            section.OriginalPoints.Add(new SectionPoint(newX, p.Z, newY, p.R, p.G, p.B));
                            section.SectionBounds.Update(newX, p.Z, newY);
                        }
                    }
                }

                points.Clear();

                if (Children != null)
                {
                    foreach (var child in Children)
                        if (child != null)
                            child.GetSectionFromSelectionWithClosest(ray, ref section, polysToCheck, deselectPolysToCheck, ref distance, ref worker, e);
                }

                return true;
            }
            else
                return false;
        }

        /// <summary>
        /// Gathers selected points within the cross section and returns them
        /// </summary>
        /// <param name="section">The <see cref="CrossSection"/> object to save points to</param>
        /// <param name="selectionZones">A List of <see cref="Polygon"/> selection zones to take points from</param>
        /// <param name="deselectionZones">A List of <see cref="Polygon"/> deselection zones to ignore points from</param>
        /// <param name="worker">The <see cref="BackgroundWorker"/> handling the cross section generation</param>
        /// <param name="e">The <see cref="DoWorkEventArgs"/> to check whether the worker has been cancelled</param>
        /// <returns>Returns true if some cross section points are found</returns>
        public bool GetSectionFromSelectionWithoutClosest(ref CrossSection section, List<ZonePolygon> selectionZones, List<ZonePolygon> deselectionZones, ref BackgroundWorker worker, DoWorkEventArgs e)
        {
            //Get all points within sectionBounds
            if (Bounds.Intersects3D(section.ClippingBox.Bounds))
            {
                if (worker.CancellationPending == true)
                {
                    e.Cancel = true;
                    return false;
                }

                var polysToCheck = new List<ZonePolygon>();
                var deselectPolysToCheck = new List<ZonePolygon>();

                foreach (var zone in selectionZones)
                {
                    if (Bounds.Intersects2D(zone.Bounds))
                        polysToCheck.Add(zone);
                }

                if (polysToCheck.Count <= 0)
                    return false;

                foreach (var zone in deselectionZones)
                {
                    if (Bounds.Intersects2D(zone.Bounds))
                        deselectPolysToCheck.Add(zone);
                }

                //Get all points within the section
                var points = LoadIntoStore();
                bool cont;

                foreach (var p in points)
                {
                    //If its within the section
                    if (section.ClippingBox.IsInside3D(p.X, p.Y, p.Z))
                    {
                        cont = false;

                        //Check its selected
                        foreach (var poly in polysToCheck)
                        {
                            if (poly.Bounds.IsInside(p.X, p.Y, p.Z) && poly.InPoly2D(p.X, p.Y, poly.Points))
                            {
                                cont = true;

                                foreach(var deselectPoly in deselectPolysToCheck)
                                {
                                    if(deselectPoly.Bounds.IsInside(p.X, p.Y, p.Z) && deselectPoly.InPoly2D(p.X, p.Y, deselectPoly.Points))
                                    {
                                        cont = false;
                                        break;
                                    }
                                }

                                break;
                            }
                        }

                        if (!cont)
                            continue;

                        //Point is selected and not deselected so add it to the section
                        double newX = 0, newY = 0;
                        Trig.DistanceAndOffsetFromLine(section.SectionCenter.X, section.SectionCenter.Y, section.Bearing, p.X, p.Y, ref newX, ref newY);
                        section.OriginalPoints.Add(new SectionPoint(newX, p.Z, newY, p.R, p.G, p.B, p.Intensity));
                        section.SectionBounds.Update(newX, p.Z, newY);
                    }
                }

                points.Clear();

                if (Children != null)
                {
                    foreach (var child in Children)
                        if (child != null)
                            child.GetSectionFromSelectionWithoutClosest(ref section, polysToCheck, deselectPolysToCheck, ref worker, e);
                }

                return true;
            }
            else
                return false;
        }

        /// <summary>
        /// Gathers points within the cross section and returns them in addtion to gathering the closest point
        /// </summary>
        /// <param name="ray">The <see cref="Ray"/> through the model towards the closet point</param>
        /// <param name="section">The <see cref="CrossSection"/> object to save points to</param>
        /// <param name="distance">The distance to the current closest point</param>
        /// <param name="worker">The <see cref="BackgroundWorker"/> handling the cross section generation</param>
        /// <param name="e">The <see cref="DoWorkEventArgs"/> to check whether the worker has been cancelled</param>
        /// <returns>Returns true if some cross section points are found</returns>
        public bool GetSectionWithClosest(Ray ray, ref CrossSection section, ref double distance, ref BackgroundWorker worker, DoWorkEventArgs e)
        {
            //First get all points within the section bounds
            if (Bounds.Intersects3D(section.ClippingBox.Bounds))
            {
                if (worker.CancellationPending == true)
                {
                    e.Cancel = true;
                    return false;
                }

                //Get all points within the section
                var points = LoadIntoStore();
                if (Bounds.IntersectsWithRay(ray))
                {
                    foreach (var p in points)
                    {
                        if (section.ClippingBox.IsInside3D(p.X, p.Y, p.Z))
                        {
                            var newDist = Vector.ClosestDistanceOnALine(ray.Origin.X, ray.Origin.Y, ray.Origin.Z, ray.End.X, ray.End.Y, ray.End.Z, p.X, p.Y, p.Z);
                            if (newDist < distance)
                            {
                                section.ClosestPoint = p;
                                distance = newDist;
                            }

                            double newX = 0, newY = 0;
                            Trig.DistanceAndOffsetFromLine(section.SectionCenter.X, section.SectionCenter.Y, section.Bearing, p.X, p.Y, ref newX, ref newY);
                            section.OriginalPoints.Add(new SectionPoint(newX, p.Z, newY, p.R, p.G, p.B, p.Intensity));
                            section.SectionBounds.Update(newX, p.Z, newY);
                        }
                    }
                }
                else
                {
                    foreach (var p in points)
                    {
                        if (section.ClippingBox.IsInside3D(p.X, p.Y, p.Z))
                        {
                            double newX = 0, newY = 0;
                            Trig.DistanceAndOffsetFromLine(section.SectionCenter.X, section.SectionCenter.Y, section.Bearing, p.X, p.Y, ref newX, ref newY);
                            section.OriginalPoints.Add(new SectionPoint(newX, p.Z, newY, p.R, p.G, p.B, p.Intensity));
                            section.SectionBounds.Update(newX, p.Z, newY);
                        }
                    }
                }
                points.Clear();

                if (Children != null)
                {
                    foreach (var child in Children)
                        if (child != null)
                            child.GetSectionWithClosest(ray, ref section, ref distance, ref worker, e);
                }

                return true;
            }
            else
                return false;
        }

        /// <summary>
        /// Gathers points within the cross section and returns them.
        /// </summary>
        /// <param name="section">The <see cref="CrossSection"/> object to save points to</param>
        /// <param name="worker">The <see cref="BackgroundWorker"/> handling the cross section generation</param>
        /// <param name="e">The <see cref="DoWorkEventArgs"/> to check whether the worker has been cancelled</param>
        /// <param name="alignment">An optional <see cref="Alignment"/> object to measure the coordinates from</param>
        /// <returns>Return true if some cross section points are found</returns>
        public bool GetSectionWithoutClosest(ref CrossSection section, ref BackgroundWorker worker, DoWorkEventArgs e, Alignment alignment = null)
        {
            //First get all points within the section bounds
            if (Bounds.Intersects3D(section.ClippingBox.Bounds))
            {
                if (worker.CancellationPending == true)
                {
                    e.Cancel = true;
                    return false;
                }

                //Get all points within the section
                var points = LoadIntoStore();
                foreach (var p in points)
                {
                    if (section.ClippingBox.IsInside3D(p.X, p.Y, p.Z))
                    {
                        if (alignment != null && section.Chainage != 1E20)
                        {
                            //Should only get an alignment when chainage is valid
                            var outVec = new AlignmentVector5();
                            if (alignment.Horizontal.GetChainageAndOffset(p, ref outVec))
                            {
                                section.OriginalPoints.Add(new SectionPoint(outVec.offset, p.Z, outVec.chainage - section.Chainage, p.R, p.G, p.B, p.Intensity));
                                section.SectionBounds.Update(outVec.offset, p.Z, outVec.chainage - section.Chainage);
                            }
                        }
                        else
                        {
                            double newX = 0, newY = 0;
                            Trig.DistanceAndOffsetFromLine(section.SectionCenter.X, section.SectionCenter.Y, section.Bearing, p.X, p.Y, ref newX, ref newY);
                            section.OriginalPoints.Add(new SectionPoint(newX, p.Z, newY, p.R, p.G, p.B, p.Intensity));
                            section.SectionBounds.Update(newX, p.Z, newY);
                        }
                    }
                }

                points.Clear();

                if (Children != null)
                {
                    foreach (var child in Children)
                    {
                        if (child != null)
                        {
                            child.GetSectionWithoutClosest(ref section, ref worker, e);
                            if (section.OriginalPoints.Count > 1000000) //Added by ES:01.03.21 - cap the number of points that can be added to the section
																		//going forward we shoulld have a some settings that  for dense medium and course.
							//if (section.OriginalPoints.Count > 1000)
							{
									return true;
                            }
                        }  
                    }
                }
                return true;
            }
            else
                return false;
        }

        /// <summary>
        /// Attempts to gather the points found within the <see cref="ClippingBox"/> of a <see cref="CrossSection"/> object
        /// </summary>
        /// <param name="section">The <see cref="CrossSection"/> object to store the points into</param>
        /// <param name="worker">The <see cref="BackgroundWorker"/> handling the process</param>
        /// <param name="e">The <see cref="DoWorkEventArgs"/> indicating wether to cancel the process or not</param>
        /// <returns>Returns true if points were found otherwise false</returns>
        public bool GetSectionPoints(ref CrossSection section, ref BackgroundWorker worker, DoWorkEventArgs e)
        {
            //Determine if the bounds of the clipping box intersect the node
            if(Bounds.Intersects3D(section.ClippingBox.Bounds))
            {
                if(worker.CancellationPending)
                {
                    e.Cancel = true;
                    return false;
                }

                //Get all points within the section
                var points = LoadIntoStore();
                foreach(var p in points)
                {
                    //If the point is within the clipping box then include it
                    if (section.ClippingBox.IsInside3D(p))
                    {
                        double newX = 0, newY = 0;
                        Trig.DistanceAndOffsetFromLine(section.SectionCenter.X, section.SectionCenter.Y, section.Bearing, p.X, p.Y, ref newX, ref newY);
                        section.OriginalPoints.Add(new SectionPoint(newX, p.Z, newY, p.R, p.G, p.B, p.Intensity));
                        section.SectionBounds.Update(newX, p.Z, newY);
                    }
                }

                points.Clear();

                //Repeat the process on each child node
                if(Children != null)
                    foreach (var child in Children)
                        child?.GetSectionPoints(ref section, ref worker, e);

                return true;
            }
            else
                return false;
        }

        /// <summary>
        /// Cheap copy of "GetSectionPoints"
        /// Attempts to gather the points found within the <see cref="ClippingBox"/> of a <see cref="CrossSection"/> object
        /// Gathers points with original coordinates and not based off dist/offset from line
        /// </summary>
        /// <param name="section">The <see cref="CrossSection"/> object to store the points into</param>
        /// <returns>Returns true if points were found otherwise false</returns>
        public bool GetSectionPointsNoWorker(ref CrossSection section) 
        {
            //Determine if the bounds of the clipping box intersect the node
            
            if (Bounds.Intersects3D(section.ClippingBox.Bounds))
            {

                //Get all points within the section
                var points = LoadIntoStore();
                foreach (var p in points)
                {
                    //If the point is within the clipping box then include it
                    if (section.ClippingBox.IsInside3D(p))
                    {
                        section.OriginalPoints.Add(new SectionPoint(p.X, p.Y, p.Z, p.R, p.G, p.B, p.Intensity));
                    }
                }
                points.Clear();

                //Repeat the process on each child node
                if (Children != null)
                    foreach (var child in Children)
                        child?.GetSectionPointsNoWorker(ref section);

                return true;
            }
            else
                return false;
        }

        /// <summary>
        /// Finds all selected points within the <see cref="OctreeNode"/>
        /// </summary>
        /// <param name="selectionZones">A List of <see cref="Polygon"/> selection zones to check</param>
        /// <param name="deselectionZones">A List of <see cref="Polygon"/> deselection zones to check</param>
        /// <returns>Returns a list of selected <see cref="CloudPoint"/> objects</returns>
        public List<CloudPoint> GetSelectedPoints(List<ZonePolygon> selectionZones, List<ZonePolygon> deselectionZones)
        {
            //Check if bounds intersects with any selection zones
            var selectedPoints = new List<CloudPoint>();
            var polysToCheck = new List<ZonePolygon>();
            var deselectPolysToCheck = new List<ZonePolygon>();

            try
            {
                //Find the selection zones that intersect with this node
                foreach (var poly in selectionZones)
                {
                    //If the zone is filtered we don't need points in it anyway
                    if (poly.Filtered)
                        continue;

                    if (Bounds.Intersects3D(poly.Bounds))
                        polysToCheck.Add(poly);
                }

                //If there are no selected zones that intersect then there are no selected points so return
                if (polysToCheck.Count <= 0)
                    return selectedPoints;

                //Find the deselection zones that intersect with this node
                foreach(var poly in deselectionZones)
                {
                    if (Bounds.Intersects3D(poly.Bounds))
                        deselectPolysToCheck.Add(poly);
                }

                //Collect the points for the node
                var points = LoadIntoStore();

                foreach(var point in points)
                {
                    bool pointSelected = false;

                    //Check if the point falls within a selection zone
                    foreach(var poly in polysToCheck)
                    {
                        if(poly.Bounds.IsInside(point.X, point.Y, point.Z) && poly.InPoly2D(point.X, point.Y, poly.Points))
                        {
                            pointSelected = true;

                            //Check if the point falls within a deselection zone
                            foreach(var deselectPoly in deselectPolysToCheck)
                            {
                                if(deselectPoly.Bounds.IsInside(point.X, point.Y, point.Z) && deselectPoly.InPoly2D(point.X, point.Y, deselectPoly.Points))
                                {
                                    pointSelected = false;
                                    break;
                                }
                            }

                            break;
                        }
                    }

                    if (!pointSelected)
                        continue;

                    //Now that we know the point is selected and is not deselected we can add it to the list
                    selectedPoints.Add(point);
                }

                return selectedPoints;
            }
            catch
            {
                return selectedPoints;
            }
        }

        public CloudPoint GetClosestPoint(Point2D point)
        {
            if(Bounds.IsInside2D(point))
            {
                CloudPoint closestPoint = null;
                double xDiff = 0, yDiff = 0, dist = 0, minDist = double.MaxValue;

                var points = LoadIntoStore();
                foreach(var p in points)
                {
                    xDiff = point.X - p.X;
                    yDiff = point.Y - p.Y;
                    dist = (xDiff * xDiff) + (yDiff * yDiff);

                    if(dist < minDist)
                    {
                        minDist = dist;
                        closestPoint = p;
                    }
                }
                points.Clear();

                //Look for closest point in each child
                if(Children != null)
                {
                    foreach(var child in Children)
                    {
                        if(child != null)
                        {
                            var closest = child.GetClosestPoint(point);
                            if (closest == null)
                                continue;

                            //Check if the new closest is closer than the current
                            xDiff = point.X - closest.X;
                            yDiff = point.Y - closest.Y;
                            dist = (xDiff * xDiff) + (yDiff * yDiff);

                            if(dist < minDist)
                            {
                                minDist = dist;
                                closestPoint = closest;
                            }
                        }
                    }
                }

                return closestPoint;
            }

            return null;
        }

        public CloudPoint GetClosestPoint(Ray ray)
        {
            if (ray == null)
                return null;

            if(Bounds.IntersectsWithRay(ray))
            {
                CloudPoint closestPoint = null;
                double dist = 0, minDist = double.MaxValue;

                var points = LoadIntoStore();
                foreach(var p in points)
                {
                    dist = Vector.ClosestDistanceOnALine(ray.Origin.X, ray.Origin.Y, ray.Origin.Z, ray.End.X, ray.End.Y, ray.End.Z, p.X, p.Y, p.Z);
                    if(dist < minDist)
                    {
                        minDist = dist;
                        closestPoint = p;
                    }
                }
                points.Clear();

                //Look for closest point in each child
                if(Children != null)
                {
                    foreach(var child in Children)
                    {
                        if(child != null)
                        {
                            var closest = child.GetClosestPoint(ray);
                            if(closest != null)
                            {
                                dist = Vector.ClosestDistanceOnALine(ray.Origin.X, ray.Origin.Y, ray.Origin.Z, ray.End.X, ray.End.Y, ray.End.Z, closest.X, closest.Y, closest.Z);
                                if(dist < minDist)
                                {
                                    minDist = dist;
                                    closestPoint = closest;
                                }
                            }
                        }
                    }
                }

                return closestPoint;
            }

            return null;
        }


        #endregion
    }

    public class ConversionProgress
    {
        #region Properties

        public ConversionStage Stage { get; set; }
        public int PrimaryPercentage { get; set; }
        public int SecondaryPercentrage { get; set; }
        public string SecondaryMessage { get; set; }
        public ulong TotalPoints { get; set; }
        public ulong CurrentPoints { get; set; }
        public ulong TotalFilePoints { get; set; }
        public ulong currentFilePoints { get; set; }

        #endregion

        #region Setup

        public ConversionProgress()
        {
            Stage = ConversionStage.PreparingFiles;
            PrimaryPercentage = 0;
            SecondaryPercentrage = 0;
            SecondaryMessage = "";
            TotalPoints = 0;
            CurrentPoints = 0;
            TotalFilePoints = 0;
            currentFilePoints = 0;
        }

        #endregion
    }

    public enum ConversionStage
    {
        PreparingFiles,
        GetBounds,
        BuildingOctree,
        FlushingOctree,
        FinalizingOctree
    }
}
