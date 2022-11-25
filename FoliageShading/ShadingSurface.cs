using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using Rhino.Geometry;

namespace FoliageShading
{
	class ShadingSurface
	{
		public bool Alive = true;
		public Double NatrualGrowthPenaltyFactor = Double.NaN;
		public int Iteration = 0;
		public Vector3d NormalDirection { get { return _normalDirection; }}
		public Vector3d FacingDirection { get { return _facingDirection; }}
		public Double TotalSunlightCapture { get { return _totalSunlightCapture; }}
		public Double Area { get { return AreaMassProperties.Compute(this.Surface, true, false, false, false).Area; } }
		public List<Point3d> LastSensorPoints;

		private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger("Default");
		private Vector3d _normalDirection;
		private Vector3d _facingDirection;
		private Double _totalSunlightCapture;

		private double previousTotalSunlighCapture = Double.NaN;
		private double previousRotateAngle = Double.NaN;

		private Random rand;


		/// <summary>
		/// The plane surface underneath. Unfortunatly subclassing doesn't trick Rhino
		/// </summary>
		public PlaneSurface Surface { get; }

		public ShadingSurface(Double naturalGrowthPenaltyFactor, Plane plane, Interval xExtents, Interval yExtents, int seed)
		{
			this.NatrualGrowthPenaltyFactor = naturalGrowthPenaltyFactor;
			this.Surface = new PlaneSurface(plane, xExtents, yExtents);
			// reparameterize
			this.Surface.SetDomain(0, new Interval(0, 1));
			this.Surface.SetDomain(1, new Interval(0, 1));

			this._normalDirection = new Vector3d(0, 0, 1); // init to facing upwards (z)
			this._facingDirection = new Vector3d(1, 0, 0); // init to facing X axis
			this._totalSunlightCapture = Double.NaN;

			this.rand = new Random(seed);
		}

		/// <summary>
		/// Unlike RotateHorizontal, the rotation here is around world z axis
		/// </summary>
		public void SetFacingDirection(double angle)
		{
			this._facingDirection.Rotate(angle, new Vector3d(0, 0, 1));
		}

		/// <summary>
		/// Does not affect FacingDirection
		/// </summary>
		public void RotateAroundNormalDirection(double angle)
		{
			bool s = this.Surface.Rotate(angle, this.NormalDirection, this.Surface.PointAt(0.5, 0.5));
			Debug.Assert(s);
		}

		public void RotateAroundFacingDirection(double angle)
		{
			bool s = this.Surface.Rotate(angle, this.FacingDirection, this.Surface.PointAt(0.5, 0.5));
			Debug.Assert(s);
		}

		public void TranslateSurface(Vector3d directionAndDistance)
		{
			this.Surface.Translate(directionAndDistance);
		}

		/// <summary>
		/// Update the shading for each iteration
		/// </summary>
		public void SetRadiationDataAndUpdate(List<Point3d> points, List<double> radiationAtPoints)
		{
			this.LastSensorPoints = points;
			this._totalSunlightCapture = radiationAtPoints.Average() * this.Area;
			Logger.Debug("total sunlight capture = " + this._totalSunlightCapture.ToString());
			Debug.Print("Before: " + radiationAtPoints.Sum().ToString() + " After: " + this._totalSunlightCapture.ToString());

			this.Turn();
			this.Grow();
			this.Survive();

			this.previousTotalSunlighCapture = this._totalSunlightCapture;
			this.Iteration++;
		}

		private void Turn()
		{
			if (Double.IsNaN(this.previousTotalSunlighCapture) || Double.IsNaN(this.previousRotateAngle))
			{
				double angle;
				if (rand.Next(0,2) == 0)
				{
					angle = 0.1;
				}
				else
				{
					angle = -0.1;
				}
				this.RotateAroundFacingDirection(angle); // this is the first iteration
				this.previousRotateAngle = angle;
			}
			else
			{
				if (this._totalSunlightCapture > this.previousTotalSunlighCapture)
				{
					this.RotateAroundFacingDirection(this.previousRotateAngle); // getting more light, so keep doing it
				}
				else // including equal case
				{
					double newAngle = -1.0 * this.previousRotateAngle * 0.8;
					this.RotateAroundFacingDirection(newAngle); // rotate back by a lesser degree
					this.previousRotateAngle = newAngle;
				}
			}
		}

		private void Grow()
		{
			//Debug.WriteLine("light: " + this._totalSunlightCapture.ToString());
			//Debug.WriteLine("area: " + this.Area.ToString());
			double sizePenalty = Math.Pow(this.Area * (8.0 + 5.0*Math.Pow(NatrualGrowthPenaltyFactor, 5)), 2.25);
			double growthFactor = 1 + Math.Tanh(this._totalSunlightCapture - sizePenalty) * 0.2;

			Plane plane;
			this.Surface.TryGetPlane(out plane);
			Transform scale1d = Transform.Scale(plane, growthFactor, 1, 1);
			this.Surface.Transform(scale1d);
		}

		private void Survive()
		{
			if (this.Area < Constants.startingShadingDepth * Constants.intervalDistanceHorizontal)
			{
				this.Alive = false;
			}
		}
	}
}
