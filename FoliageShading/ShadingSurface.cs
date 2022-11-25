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
		public Vector3d NormalDirection { get { return _normalDirection; }}
		public Vector3d FacingDirection { get { return _facingDirection; }}
		public Double TotalSunlightCapture { get { return _totalSunlightCapture; }}
		public Double Area { get { return AreaMassProperties.Compute(this.Surface, true, false, false, false).Area; } }

		private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger("Default");
		private Vector3d _normalDirection;
		private Vector3d _facingDirection;
		private Double _totalSunlightCapture;

		//private bool isAnglePass = true;
		private double previousTotalSunlighCapture = Double.NaN;
		private double previousRotateAngle = Double.NaN;

		private Random rand;


		/// <summary>
		/// The plane surface underneath. Unfortunatly subclassing doesn't trick Rhino
		/// </summary>
		public PlaneSurface Surface { get; }

		public ShadingSurface(Plane plane, Interval xExtents, Interval yExtents, int seed)
		{
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
			//Debug.WriteLine(points.Count);
			this._totalSunlightCapture = radiationAtPoints.Sum();
			//Debug.WriteLine(this._totalSunlightCapture);
			Logger.Debug("total sunlight capture = " + this._totalSunlightCapture.ToString());

			this.Turn();
			this.Grow();

			this.previousTotalSunlighCapture = this._totalSunlightCapture;
		}

		private void Turn()
		{
			if (Double.IsNaN(this.previousTotalSunlighCapture) || Double.IsNaN(this.previousRotateAngle))
			{
				double angle = 0;
				if (rand.Next(0,2) == 0)
				{
					angle = 0.1;
				}
				else
				{
					angle = -0.1;
				}
				//Debug.WriteLine(angle);
				this.RotateAroundFacingDirection(angle); // this is the first iteration
				this.previousRotateAngle = angle;
			}
			else
			{
				if (this._totalSunlightCapture > this.previousTotalSunlighCapture)
				{
					this.RotateAroundFacingDirection(this.previousRotateAngle); // getting more light, so keep doing it
				}
				else
				{
					double newAngle = -1.0 * this.previousRotateAngle * 0.6;
					this.RotateAroundFacingDirection(newAngle); // rotate back by a lesser degree
					this.previousRotateAngle = newAngle;
				}
			}
		}

		private void Grow()
		{
			if (this._totalSunlightCapture > 900 * 4)
			{
				Plane plane;
				this.Surface.TryGetPlane(out plane);
				Transform scale1d = Transform.Scale(plane, 1.1, 1, 1);
				this.Surface.Transform(scale1d);
			}
		}
	}
}
