namespace MatrixMultiple
{
	public class Matrix
	{
		public int Rows;	//row
		public int Columns;	//cols

		public Matrix(int n, int m)
		{
			Rows = n;
			Columns = m;

			Data = new double[n][];

			for (var i = 0; i < n; i++)
			{
				Data[i] = new double[m];
			}
		}

		public double[][] Data { get; set; }
	}
}
