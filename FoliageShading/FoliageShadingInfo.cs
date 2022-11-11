using Grasshopper;
using Grasshopper.Kernel;
using System;
using System.Drawing;

namespace FoliageShading
{
	public class FoliageShadingInfo : GH_AssemblyInfo
	{
		public override string Name => "FoliageShading";

		//Return a 24x24 pixel bitmap to represent this GHA library.
		public override Bitmap Icon => null;

		//Return a short string describing the purpose of this GHA library.
		public override string Description => "";

		public override Guid Id => new Guid("caad028e-e45b-4a04-8275-db24e4c48008");

		//Return a string identifying you or your company.
		public override string AuthorName => "Jialiang Xiang";

		//Return a string representing your preferred contact details.
		public override string AuthorContact => "Jialiangxiang@outlook.com";
	}
}