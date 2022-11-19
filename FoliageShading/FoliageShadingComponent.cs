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
		/// <summary>
		/// Each implementation of GH_Component must provide a public 
		/// constructor without any arguments.
		/// Category represents the Tab in which the component will appear, 
		/// Subcategory the panel. If you use non-existing tab or panel names, 
		/// new tabs/panels will automatically be created.
		/// </summary>
		public FoliageShadingComponent()
		  : base("FoliageShading", "Foliage",
			"Description",
			"Final Project", "Generate Shading")
		{
		}

		/// <summary>
		/// Registers all the input parameters for this component.
		/// </summary>
		protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
		{
			// You can often supply default values when creating parameters.
			// All parameters must have the correct access type. If you want to import lists or trees of values, modify the ParamAccess flag.

			pManager.AddGeometryParameter("Base Surfaces", "B", "The areas to fill in with shadings", GH_ParamAccess.list);
			pManager.AddNumberParameter("Interval Distance", "I", "Horizontal distance between two shadings, in the model unit", GH_ParamAccess.item);

			//pManager[0].Optional = true;
		}

		/// <summary>
		/// Registers all the output parameters for this component.
		/// </summary>
		protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
		{
			// Output parameters do not have default values, but they too must have the correct access type.

			pManager.AddCurveParameter("Support Wires", "W", "the wires that support the shadings", GH_ParamAccess.list);
			//pManager.AddSurfaceParameter("Shadings", "S", "the many pieces of the shadings generated", GH_ParamAccess.list);

			//pManager.HideParameter(0);
		}

		/// <summary>
		/// This is the method that actually does the work.
		/// </summary>
		/// <param name="DA">The DA object can be used to retrieve data from input parameters and 
		/// to store data in output parameters.</param>
		protected override void SolveInstance(IGH_DataAccess DA)
		{
			List<Surface> inputGeometries = new List<Surface>();
			Double interval = Double.NaN; 

			if (!DA.GetDataList(0, inputGeometries)) return;
			if (!DA.GetData(1, ref interval)) return;

			if (interval <= 0)
			{
				AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Interval must be positive");
				return;
			}

			List<Curve> wires = new List<Curve>();
			foreach (Surface s in inputGeometries)
			{
				wires.AddRange(this.CreateCenterLines(s, interval));
			}
			
			DA.SetDataList(0, wires);
		}

		List<Surface> CreateStartingShadings()
		{
			throw new NotImplementedException();
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
			double intervalInU = 1.0 / numberOfCenterLines; // number of intervals = number of center lines

			double padding = intervalInU / 2.0; // padding before the first center line and after the last one

			List<Curve> isoCurves = new List<Curve>();
			for (int i = 0; i < numberOfCenterLines; i++)
			{
				isoCurves.Add(baseSurface.IsoCurve(1, i * intervalInU + padding));
			}

			return isoCurves;
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