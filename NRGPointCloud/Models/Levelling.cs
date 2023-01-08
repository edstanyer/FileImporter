using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NRG.Models
{

	//|             ______
	//|<------------|____|
	//|				  /\
	//|				 /	\
	//|				/	 \

	public class BackSight
	{
		private double openinglevel = 0;
		private double staffReading = 0;


		public BackSight(double openingLevel)
		{
			openinglevel = openingLevel;
		}

		public double OpeningLevel
		{
			get { return openinglevel; }
			set
			{
				openinglevel = value;

			}
		}


		public double StaffReading
		{
			get { return staffReading; }
			set
			{
				staffReading = value;

			}
		}

		public double CollimationHeight
		{
			get
			{
				return CalcCollimation();
			}
		}

		private double CalcCollimation()
		{
			return openinglevel + staffReading;
		}

	}

	//| ______			   |
	//| |____| ----------->|
	//|	  /\			   |
	//|  /	\			   |
	//| /	 \			   |

	public class ForeSight
	{
		private double staffreading = 0;
		private BackSight backsight;
		public string remark;
		#region setup
		public ForeSight(BackSight backSight)
		{
			backsight = backSight;
		}
		#endregion setup

		public ForeSight(double StaffReading, string Remark)
		{
			staffreading = StaffReading;
			remark = Remark;
		}

		public double StaffReading
		{

			get { return staffreading; }
			set
			{
				staffreading = value;
			}
		}

		public double ReducedLevel
		{

			//if (backsight != null) {
				get {return backsight.CollimationHeight - staffreading; }

			//else
				//return 0;
		}

		public BackSight BackSight
		{
			get { return backsight; }
			set { backsight = value; }
		}
	}

	public class LevelBay
	{
		private double StartLevel = 0;
		private BackSight backSight;
		public List<ForeSight> Intermediates = new List<ForeSight>();
		private ForeSight foreSight;

		public LevelBay(double OpeningLevel)
		{
			StartLevel = OpeningLevel;
			backSight = new BackSight(OpeningLevel);
		}

		public double ReducedLevel 
		{
			get 
			{
				if (ForeSight != null)
				{
					return foreSight.ReducedLevel;
				}
				else
				{
					return 0;
				}
			}
		}

		public double OpeningLevel
		{
			get { return backSight.OpeningLevel; }
			set
			{
				backSight.OpeningLevel = value;
			}
		}

		public BackSight  BackSight
		{
			get { return backSight; }
			set
			{
				backSight = value;	
			}
		}

		public ForeSight ForeSight
		{
			get { return foreSight; }
			set
			{
				foreSight = value;
			}
		}

		public void AddIntermediate(ForeSight Intermediate, int ElementIndex = -1)
		{
			if (Intermediates == null)
			{
				Intermediates = new List<ForeSight>();
			}
			if (ElementIndex < 0 || ElementIndex > Intermediates.Count)
			{
				Intermediates.Add(Intermediate);				
			}
			else 
			{
				Intermediates.Insert(ElementIndex, Intermediate);
			}
		}


	}

	public class LevelingRun
	{
		public string Title = "";
		public string Date = "";

		private double openinglevel=0;
		

		public List <LevelBay> LevelBays= new List<LevelBay>();

		public double OpeningLevel
		{
			get { return openinglevel; }
			set
			{
				openinglevel = value;
				RecomputeRun();
			}
		}

		public void RecomputeRun()
		{
			if (LevelBays != null && LevelBays.Count > 0)
			{
				double lev = openinglevel;
				for (int i = 1; i < LevelBays.Count; i++)
				{
					
				}
			}
		}

	}
}
