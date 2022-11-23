using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino.Geometry;

namespace FoliageShading
{
	class ShadingSurface: PlaneSurface
	{
		private Vector3d _normalDirection;
		public Vector3d NormalDirection 
		{
			get { return _normalDirection; }
			set { _normalDirection = value; } 
		}
		private Vector3d _facingDirection;
		public Vector3d FacingDirection
		{
			get { return _facingDirection; }
		}
		
		public ShadingSurface(Plane plane, Interval xExtents, Interval yExtents) : base(plane, xExtents, yExtents)
		{
			this._normalDirection = new Vector3d(0, 0, 1); // init to facing upwards (z)
			this._facingDirection = new Vector3d(1, 0, 0); // init to facing X axis

			// reparameterize
			this.SetDomain(0, new Interval(0, 1));
			this.SetDomain(1, new Interval(0, 1));
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
			bool s = this.Rotate(angle, this.NormalDirection, this.PointAt(0.5, 0.5));
			Debug.Assert(s);
		}

		public void RotateAroundFacingDirection(double angle)
		{
			bool s = this.Rotate(angle, this.FacingDirection, this.PointAt(0.5, 0.5));
			Debug.Assert(s);
		}
	}
}
