using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino.Geometry;

namespace FoliageShading
{
	class ShadingSurface
	{
		private Vector3d _normalDirection;
		public Vector3d NormalDirection 
		{
			get { return _normalDirection; }
		}
		private Vector3d _facingDirection;
		public Vector3d FacingDirection
		{
			get { return _facingDirection; }
		}

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
	}
}
