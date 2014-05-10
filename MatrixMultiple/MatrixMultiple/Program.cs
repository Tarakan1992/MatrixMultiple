using System;
using MPI;

namespace MatrixMultiple
{
	enum MessageType
	{
		OffsetRow = 10,
		Rows = 20,
		MatrixA = 30,
		MatrixB = 40,
		MatrixC = 50
	}

	class Program
	{
		static void Main(string[] args)
		{
			var rowsA = int.Parse(args[0]);
			var columnA = int.Parse(args[1]);
			var columnB = int.Parse(args[2]);
			

			using (new MPI.Environment(ref args))
			{
				var comm = Communicator.world;

				if (comm.Rank == 0)
				{
					var matrixA = new Matrix(rowsA, columnA);
					var matrixB = new Matrix(columnA, columnB);
					var matrixC = new Matrix(rowsA, columnB);

					var matrixManager = new MatrixManager();

					matrixManager.SimpleInitialization(matrixA);
					matrixManager.SimpleInitialization(matrixB);
					
					matrixManager.Print(matrixA);
					Console.Write("\n\n");
					
					matrixManager.Print(matrixB);
					Console.Write("\n\n");

					int numberOfSlaves = comm.Size - 1;
					var start = DateTime.Now;

					int rowsPerSlave = rowsA / numberOfSlaves;
					int remainingRows = rowsA % numberOfSlaves;
					int offsetRow = 0;

					for (var destination = 1; destination <= numberOfSlaves; destination++)
					{
						int rows = (destination <= remainingRows) ? rowsPerSlave + 1 : rowsPerSlave;

						var offsetRowRequest = comm.ImmediateSend(offsetRow, destination, (int)MessageType.OffsetRow);
						

						var rowRequest = comm.ImmediateSend(rows, destination, (int)MessageType.Rows);

						var temp = new double[rows][];

						for (var i = 0; i < rows; i++)
						{
							temp[i] = matrixA.Data[offsetRow];
						}

						var matrixRowRequest = comm.ImmediateSend(temp, destination, (int)MessageType.MatrixA);

						var b = matrixB.Data;

						var matrixBRequest = comm.ImmediateSend(b, destination, (int)MessageType.MatrixB);

						offsetRowRequest.Wait();
						rowRequest.Wait();
						matrixRowRequest.Wait();
						matrixBRequest.Wait();

						offsetRow = offsetRow + rows;
					}

					for (var source = 1; source <= numberOfSlaves; source++)
					{
						var offsetRowRequest = comm.ImmediateReceive<int>(source, (int)MessageType.OffsetRow);
						
						var rows = comm.ImmediateReceive<int>(source, (int)MessageType.Rows);

						var temp = comm.ImmediateReceive<double[][]>(source, (int)MessageType.MatrixC);
						
						offsetRowRequest.Wait();
						rows.Wait();
						temp.Wait();


						for (var i = 0; i < (int)rows.GetValue(); i++)
						{
							matrixC.Data[(int)offsetRowRequest.GetValue() + i] = ((double[][])temp.GetValue())[i];
						}
					}

					DateTime end = DateTime.Now;
					matrixManager.Print(matrixC);
					Console.Write("\n\n");
					Console.Write(end - start);
				}
				else
				{
					const int source = 0;
					var offsetRow = comm.ImmediateReceive<int>(source, (int)MessageType.OffsetRow);
					
					var rows = comm.ImmediateReceive<int>(source, (int)MessageType.Rows);
					
					var a = comm.ImmediateReceive<double[][]>(source, (int)MessageType.MatrixA);
					
					var b = comm.ImmediateReceive<double[][]>(source, (int)MessageType.MatrixB);
					
					offsetRow.Wait();
					rows.Wait();
					a.Wait();
					b.Wait();
					
					
					var c = new double[(int)rows.GetValue()][];

					for (var i = 0; i < (int)rows.GetValue(); i++)
					{
						c[i] = new double[columnB];
					}

					for (var k = 0; k < columnB; k++)
					{
						for (var i = 0; i < (int)rows.GetValue(); i++)
						{
							c[i][k] = 0.0;
							for (var j = 0; j < columnA; j++)
							{
								c[i][k] = c[i][k] + ((double[][])a.GetValue())[i][j] * ((double[][])b.GetValue())[j][k];
							}
						}
					}

					var offsetRequest = comm.ImmediateSend((int)offsetRow.GetValue(), source, (int)MessageType.OffsetRow);
					
					var rowsRequest = comm.ImmediateSend((int)rows.GetValue(), source, (int)MessageType.Rows);
					
					var matrixCRequest = comm.ImmediateSend(c, source, (int)MessageType.MatrixC);

					offsetRequest.Wait();
					rowsRequest.Wait();
					matrixCRequest.Wait();
				}
			}
		}
	}
}
