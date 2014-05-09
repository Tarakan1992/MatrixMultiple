using System;

namespace MatrixMultiple
{
	public class MatrixManager
	{
		public Matrix Multiple(Matrix a, Matrix b)
		{
			if (a.Columns != b.Rows)
			{
				throw new Exception("Can't multiple matrix!");
			}

			var resultMatrix = new Matrix(a.Rows, b.Columns);

			for (var i = 0; i < a.Rows; i++)
			{
				for (var j = 0; j < b.Columns; j++)
				{
					double sum = 0;
					for (var k = 0; k < a.Columns; k++)
					{
						sum += a.Data[i][k]*b.Data[k][j];
					}

					resultMatrix.Data[i][j] = sum;
				}
			}

			return resultMatrix;
		}

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
	}
}
