using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace NRG.Import
{
    public sealed class PointCloudFileDeserializer : SerializationBinder
    {
        #region Override Methods

        public override Type BindToType(string assemblyName, string typeName)
        {
            Type deserializeType = null;
            deserializeType = Type.GetType(typeName.Replace("NRGPointCloud", "NRG").Replace("Models.Vector3", "Services.OldFileVersions.Point3D").Replace("Models.Bounds", "Services.OldFileVersions.Bounds").Replace("Export.PointCloudFile", "Services.OldFileVersions.PointCloudFile"));

            return deserializeType;
        }

        #endregion
    }
}
