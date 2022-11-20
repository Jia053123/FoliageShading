using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace FoliageShading
{
	public class FoliageShadingComponent : GH_Component
	{
		private Double startingShadingDepth = 1.0;
		private bool hasPointsData = false;
		private bool hasResultsData = false;
		private String logOutput = "";
		private int iteration = 0;

		/// <summary>
		/// Each implementation of GH_Component must provide a public constructor without any arguments.
		/// Category represents the Tab in which the component will appear, Subcategory the panel. If you use non-existing tab or panel names, new tabs/panels will automatically be created.
		/// </summary>
		public FoliageShadingComponent()
		  : base("FoliageShading", "Foliage",
			"Description",
			"Final Project", "Generate Shading")
		{
		}

		protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
		{
			pManager.AddGeometryParameter("Base Surfaces", "B", "The areas to fill in with shadings", GH_ParamAccess.list);
			pManager.AddNumberParameter("Interval Distance", "ID", "Horizontal distance between two shadings, in the model unit", GH_ParamAccess.item);
			pManager.AddNumberParameter("Growth Point Interval", "GPI", "Vertical distance between two growth points, influencing density", GH_ParamAccess.item);
			pManager.AddPointParameter("Points for Ladybug Incident Radiation simulation", "P", "The points where the simulation is done for Ladybug Incident Radiation component", GH_ParamAccess.list);
			pManager.AddNumberParameter("Ladybug Incident Radiation Results", "LIR", "The 'results' output from Ladybug Incident Radiation component", GH_ParamAccess.list);

			pManager[3].Optional = true;
			pManager[4].Optional = true;
		}

		protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
		{
			pManager.AddCurveParameter("Support Wires", "W", "The wires that support the shadings", GH_ParamAccess.list);
			pManager.AddSurfaceParameter("Shadings", "S", "The many pieces of the shadings generated", GH_ParamAccess.list);
			pManager.AddNumberParameter("Grid Size for Ladybug Incident Radiation", "GS", "The grid size to use for Ladybug Incident Radiation simulations", GH_ParamAccess.item);
			pManager.AddTextParameter("Log", "L", "Information about the state of the compoment", GH_ParamAccess.item);

			//pManager.HideParameter(0);
		}

		/// <param name="DA">The DA object can be used to retrieve data from input parameters and 
		/// to store data in output parameters.</param>
		protected override void SolveInstance(IGH_DataAccess DA)
		{
			List<Surface> baseSurfaces = new List<Surface>();
			Double interval = Double.NaN; 
			Double growthPointInterval = Double.NaN;
			List<Point3d> radiationPoints = new List<Point3d>();
			List<Double> radiationResults = new List<Double>();

			if (!DA.GetDataList(0, baseSurfaces)) return;
			if (!DA.GetData(1, ref interval)) return;
			if (!DA.GetData(2, ref growthPointInterval)) return;

			if (DA.GetDataList(3, radiationPoints) && radiationPoints.Count > 1)
			{
				this.hasPointsData = true;
				logOutput += Environment.NewLine + "Points data received: count = " + radiationPoints.Count.ToString();
			}
			else
			{
				this.hasPointsData = false;
				logOutput += Environment.NewLine + "No points data received";
			}

			if (DA.GetDataList(4, radiationResults) && radiationResults.Count > 1)
			{
				this.hasResultsData = true;
				logOutput += Environment.NewLine + "Results data receieved: count = " + radiationResults.Count.ToString();
			}
			else
			{
				this.hasResultsData = false;
				logOutput += Environment.NewLine + "No results data receieved"; 
			}

			if (interval <= 0)
			{
				AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Interval must be positive");
				return;
			}

			List<Curve> centerLines = new List<Curve>();
			foreach (Surface s in baseSurfaces)
			{
				centerLines.AddRange(this.CreateCenterLines(s, interval));
			}

			List<PlaneSurface> shadings = new List<PlaneSurface>();
			foreach (Curve cl in centerLines)
			{
				List<Point3d> growthPoints = this.CreateGrowthPoints(cl, growthPointInterval);
				shadings.AddRange(this.CreateStartingShadingPlanes(growthPoints, 1.0, interval, baseSurfaces.First().NormalAt(0, 0)));
			}

			double gridSize = this.startingShadingDepth;

			logOutput += Environment.NewLine + iteration.ToString();

			DA.SetDataList(0, centerLines);
			DA.SetDataList(1, shadings);
			DA.SetData(2, gridSize);
			DA.SetData(3, logOutput);

			iteration += 1;
		}

		List<Curve> CreateCenterLines(Surface baseSurface, double intervalDist)
		{
			// reparameterize
			bool s1 = baseSurface.SetDomain(0, new Interval(0, 1));
			Debug.Assert(s1);
			bool s2 = baseSurface.SetDomain(1, new Interval(0, 1));
			Debug.Assert(s2);

			double width, height;
			baseSurface.GetSurfaceSize(out width, out height);
			int numberOfCenterLines = (int) Math.Floor(width / intervalDist); // no lines at either ends because they mark the centers of the shadings
			double intervalInU = 1.0 / numberOfCenterLines; 

			double padding = intervalInU / 2.0; // padding before the first center line and after the last one

			List<Curve> isoCurves = new List<Curve>();
			for (int i = 0; i < numberOfCenterLines; i++)
			{
				isoCurves.Add(baseSurface.IsoCurve(1, i * intervalInU + padding));
			}

			return isoCurves;
		}

		List<Point3d> CreateGrowthPoints(Curve centerLine, Double growthPointInterval)
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

		List<PlaneSurface> CreateStartingShadingPlanes(List<Point3d> growthPoints, double startingShadingDepth, double intervalDistance, Vector3d outsideDirection)
		{
			List<PlaneSurface> startingSurfaces = new List<PlaneSurface>();
			foreach (Point3d gp in growthPoints)
			{
				PlaneSurface surface = new PlaneSurface(Plane.WorldXY, new Interval(-1 * startingShadingDepth / 2.0, startingShadingDepth / 2.0), new Interval(-1 * intervalDistance / 2.0, intervalDistance / 2.0));

				Vector3d defaultDirection = new Vector3d(1, 0, 0); // faces X axis when the surface was created 
				double rotationAngle = Math.Atan2(outsideDirection.Y * defaultDirection.X - outsideDirection.X * defaultDirection.Y, outsideDirection.X * defaultDirection.X + outsideDirection.Y * defaultDirection.Y);
				bool s = surface.Rotate(rotationAngle, new Vector3d(0, 0, 1), new Point3d(0, 0, 0));
				Debug.Assert(s);

				Vector3d translation = new Vector3d(gp); // the vector points from 0,0,0 to gp
				surface.Translate(translation);

				startingSurfaces.Add(surface);
			}

			return startingSurfaces;
		}

		/// <summary>
		/// The Exposure property controls where in the panel a component icon 
		/// will appear. There are seven possible locations (primary to septenary), 
		/// each of which can be combined with the GH_Exposure.obscure flag, which 
		/// ensures the component will only be visible on panel dropdowns.
		/// </summary>
		public override GH_Exposure Exposure => GH_Exposure.primary;

		/// <summary>
		/// Provides an Icon for every component that will be visible in the User Interface.
		/// Icons need to be 24x24 pixels.
		/// You can add image files to your project resources and access them like this:
		/// return Resources.IconForThisComponent;
		/// </summary>
		protected override System.Drawing.Bitmap Icon => null;

		/// <summary>
		/// Each component must have a unique Guid to identify it. 
		/// It is vital this Guid doesn't change otherwise old ghx files 
		/// that use the old ID will partially fail during loading.
		/// </summary>
		public override Guid ComponentGuid => new Guid("3de06452-1374-475f-8a9f-b54cf4b94e09");
	}
}