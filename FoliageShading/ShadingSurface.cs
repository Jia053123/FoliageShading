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
		private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger("Default");
		private Vector3d _normalDirection;
		private Vector3d _facingDirection;
		private Double _totalSunlightCapture;
		public Vector3d NormalDirection { get { return _normalDirection; }}
		public Vector3d FacingDirection { get { return _facingDirection; }}
		public Double TotalSunlightCapture { get { return _totalSunlightCapture; }}
		public Double Area { get { return AreaMassProperties.Compute(this.Surface, true, false, false, false).Area; } }
		

		/// <summary>
		/// The plane surface underneath. Unfortunatly subclassing doesn't trick Rhino
		/// </summary>
		public PlaneSurface Surface { get; }

		public ShadingSurface(Plane plane, Interval xExtents, Interval yExtents)
		{
			this.Surface = new PlaneSurface(plane, xExtents, yExtents);
			// reparameterize
			this.Surface.SetDomain(0, new Interval(0, 1));
			this.Surface.SetDomain(1, new Interval(0, 1));

			this._normalDirection = new Vector3d(0, 0, 1); // init to facing upwards (z)
			this._facingDirection = new Vector3d(1, 0, 0); // init to facing X axis
			this._totalSunlightCapture = Double.NaN;
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
			this._totalSunlightCapture = radiationAtPoints.Sum();
			Logger.Debug("total sunlight capture = " + this._totalSunlightCapture.ToString());

		}
	}
}
