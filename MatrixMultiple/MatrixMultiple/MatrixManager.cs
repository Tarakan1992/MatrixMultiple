﻿using System;

namespace MatrixMultiple
{
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
	}
}
