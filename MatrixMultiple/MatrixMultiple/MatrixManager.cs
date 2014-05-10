using System;

namespace MatrixMultiple
{
	using System.IO;
	using System.Runtime.InteropServices;

	public class MatrixManager
	{
		public void Print(Matrix matrix)
		{
			for (var i = 0; i < matrix.Rows; i++)
			{
				for (var j = 0; j < matrix.Columns; j++)
				{
					Console.Write("{0:0.##} ", matrix.Data[i][j]);
				}

				Console.Write("\n");
			}
		}

		public void RandomInitialization(Matrix matrix)
		{
			var r = new Random();
			for (var i = 0; i < matrix.Rows; i++)
			{
				for (var j = 0; j < matrix.Columns; j++)
				{
					matrix.Data[i][j] = r.NextDouble();
				}
			}
		}

		public void SimpleInitialization(Matrix matrix)
		{
			for (var i = 0; i < matrix.Rows; i++)
			{
				for (var j = 0; j < matrix.Columns; j++)
				{
					matrix.Data[i][j] = 1.0;
				}
			}
		}

		public Matrix FileInitialization(string filename)
		{
			using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
			{
				var buff = new byte[255];
				
				fs.Read(buff, 0, sizeof(int));

				int n = BitConverter.ToInt32(buff, 0);

				fs.Read(buff, 0, sizeof(int));

				int m = BitConverter.ToInt32(buff, 0);

				var matrix = new Matrix(n, m);

				for (var i = 0; i < n; i++)
				{
					for (var j = 0; j < m; j++)
					{
						fs.Read(buff, 0, sizeof (double));
						matrix.Data[i][j] = BitConverter.ToDouble(buff, 0);
					}
				}

				return matrix;
			}
		}
	}
}
