using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
			foreach (Curve cl in centerLines)
			{
				List<Point3d> growthPoints = this.CreateGrowthPoints(cl, growthPointInterval);
				shadings.AddRange(this.CreateStartingShadingPlanes(growthPoints, startingShadingDepth, intervalDist, baseSurfaces.First().NormalAt(0, 0)));
			}

			this._shadingSurfaces = shadings;
			this._centerLines = centerLines;

			this._isInitialized = true;
		}

		public void UpdateSurfacesWithRadiationData(List<Point3d> sensorPoints, List<double> radiationDataAtPoints)
		{
			Debug.Assert(sensorPoints.Count == radiationDataAtPoints.Count);
			double roughNumOfPointsForEachShading = sensorPoints.Count / this._shadingSurfaces.Count;
			int numOfPointsForEachShading = (int) Math.Floor(roughNumOfPointsForEachShading);
			Debug.Assert(numOfPointsForEachShading == roughNumOfPointsForEachShading);

			for (int i = 0; i < this._shadingSurfaces.Count; i++)
			{
				var s = this._shadingSurfaces[i];
				int startIndex = i * numOfPointsForEachShading;
				int pointCount = numOfPointsForEachShading;
				s.SetRadiationDataAndUpdate(sensorPoints.GetRange(startIndex, pointCount), radiationDataAtPoints.GetRange(startIndex, pointCount));
			}
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

		private List<Point3d> CreateGrowthPoints(Curve centerLine, Double growthPointInterval)
		{
			// reparameterize
			centerLine.Domain = new Interval(0, 1);

			double totalHeight = centerLine.GetLength();
			int numberOfGrowthPoints = (int)Math.Floor(totalHeight / growthPointInterval); // no points at either ends 
			double growthPointIntervalInV = 1.0 / numberOfGrowthPoints;

			double padding = growthPointIntervalInV / 2.0;

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
			foreach (Point3d gp in growthPoints)
			{
				ShadingSurface surface = new ShadingSurface(Plane.WorldXY, new Interval(-1 * startingShadingDepth / 2.0, startingShadingDepth / 2.0), new Interval(-1 * intervalDistance / 2.0, intervalDistance / 2.0));

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
