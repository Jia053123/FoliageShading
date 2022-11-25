using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino;
using Rhino.Geometry;

namespace FoliageShading
{
	class ShadingsManager
	{
		private List<ShadingSurface> _shadingSurfaces;
		private List<Curve> _centerLines;
		private bool _isInitialized = false;
		public List<ShadingSurface> ShadingSurfaces { get { return this._shadingSurfaces; } }
		public List<Curve> CenterLines { get { return this._centerLines; } }
		public bool IsInitialized { get { return this._isInitialized; } }

		public void InitializeShadingSurfaces(List<Surface> baseSurfaces, Double intervalDist, Double growthPointInterval, Double startingShadingDepth)
		{
			List<Curve> centerLines = new List<Curve>();
			foreach (Surface s in baseSurfaces)
			{
				centerLines.AddRange(this.CreateCenterLines(s, intervalDist));
			}

			List<ShadingSurface> shadings = new List<ShadingSurface>();
			bool isEvenIndex = true;
			foreach (Curve cl in centerLines)
			{
				List<Point3d> growthPoints = this.CreateGrowthPoints(isEvenIndex, cl, growthPointInterval);
				shadings.AddRange(this.CreateStartingShadingPlanes(growthPoints, startingShadingDepth, intervalDist, baseSurfaces.First().NormalAt(0, 0)));
				isEvenIndex = !isEvenIndex;
			}

			this._shadingSurfaces = shadings;
			this._centerLines = centerLines;

			this._isInitialized = true;
		}

		public void UpdateSurfacesWithRadiationData(List<Point3d> sensorPoints, List<double> radiationDataAtPoints)
		{
			Debug.Assert(sensorPoints.Count == radiationDataAtPoints.Count);
			
			foreach (ShadingSurface ss in this._shadingSurfaces)
			{
				List<Point3d> sps = new List<Point3d>();
				List<double> rdaps = new List<double>();
				List<int> indexesAlreadyAdded = new List<int>();

				for (int i = 0; i < sensorPoints.Count; i++)
				{
					if (!indexesAlreadyAdded.Contains(i)) // each point belongs only to one surface
					{
						var p = sensorPoints[i];
						if (this.IsPointOnSurface(p, ss.Surface))
						{
							sps.Add(p);
							rdaps.Add(radiationDataAtPoints[i]);
							indexesAlreadyAdded.Add(i); 
						}
					}
				}
				ss.SetRadiationDataAndUpdate(sps, rdaps);
			}
		}

		private bool IsPointOnSurface(Point3d point, Surface surface)
		{
			double u, v;
			surface.ClosestPoint(point, out u, out v);
			var surf_p = surface.PointAt(u, v);

			return surf_p.DistanceTo(point) < RhinoDoc.ActiveDoc.ModelAbsoluteTolerance + 0.33; // default offset is 10cm = 0.328084... feet; refactor ActiveDoc if porting to Mac
		}

		private List<Curve> CreateCenterLines(Surface baseSurface, double intervalDist)
		{
			// reparameterize
			bool s1 = baseSurface.SetDomain(0, new Interval(0, 1));
			Debug.Assert(s1);
			bool s2 = baseSurface.SetDomain(1, new Interval(0, 1));
			Debug.Assert(s2);

			double width, height;
			baseSurface.GetSurfaceSize(out width, out height);
			int numberOfCenterLines = (int)Math.Floor(width / intervalDist); // no lines at either ends because they mark the centers of the shadings
			double intervalInU = 1.0 / numberOfCenterLines;

			double padding = intervalInU / 2.0; // padding before the first center line and after the last one

			List<Curve> isoCurves = new List<Curve>();
			for (int i = 0; i < numberOfCenterLines; i++)
			{
				isoCurves.Add(baseSurface.IsoCurve(1, i * intervalInU + padding));
			}

			return isoCurves;
		}

		private List<Point3d> CreateGrowthPoints(bool isEvenIndex, Curve centerLine, Double growthPointInterval)
		{
			// reparameterize
			centerLine.Domain = new Interval(0, 1);

			double totalHeight = centerLine.GetLength();
			int numberOfGrowthPoints = (int)Math.Floor(totalHeight / growthPointInterval) - 1; // no points at either ends; minus 1 for shifting 
			double growthPointIntervalInV = 1.0 / numberOfGrowthPoints;

			double padding;
			if (isEvenIndex)
			{
				padding = growthPointIntervalInV * 0.25;
			}
			else
			{
				padding = growthPointIntervalInV * 0.75;
			}
			

			List<Point3d> growthPoints = new List<Point3d>();
			for (int i = 0; i < numberOfGrowthPoints; i++)
			{
				growthPoints.Add(centerLine.PointAt(i * growthPointIntervalInV + padding));
			}

			return growthPoints;
		}

		private List<ShadingSurface> CreateStartingShadingPlanes(List<Point3d> growthPoints, double startingShadingDepth, double intervalDistance, Vector3d outsideDirection)
		{
			List<ShadingSurface> startingSurfaces = new List<ShadingSurface>();
			var rand = new Random();
			foreach (Point3d gp in growthPoints)
			{
				ShadingSurface surface = new ShadingSurface(Plane.WorldXY, new Interval(-1 * startingShadingDepth / 2.0, startingShadingDepth / 2.0), new Interval(-1 * intervalDistance / 2.0, intervalDistance / 2.0), rand.Next()-1);

				Vector3d defaultDirection = surface.FacingDirection;
				double rotationAngle = Math.Atan2(outsideDirection.Y * defaultDirection.X - outsideDirection.X * defaultDirection.Y, outsideDirection.X * defaultDirection.X + outsideDirection.Y * defaultDirection.Y);
				surface.SetFacingDirection(rotationAngle);
				surface.RotateAroundNormalDirection(rotationAngle);

				Vector3d translation = new Vector3d(gp); // the vector points from 0,0,0 to gp
				surface.TranslateSurface(translation);

				startingSurfaces.Add(surface);
			}

			return startingSurfaces;
		}
	}
}
