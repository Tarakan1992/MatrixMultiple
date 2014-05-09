using System;
using MPI;

namespace MatrixMultiple
{
	enum MessageType
	{
		FromMaster = 10,
		FromSlave = 20
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

				//Console.Write("Rank = {0}. Time:{1}\n", comm.Rank, DateTime.Now.ToString("mm:ss.ffff"));

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

						//Console.Write("Send to {0}-worker {1} rows. Time:{2}\n", destination, rows, DateTime.Now.ToString("mm:ss.ffff"));

						var request = comm.ImmediateSend(offsetRow, destination, (int)MessageType.FromMaster);
						request.Wait();

						request = comm.ImmediateSend(rows, destination, (int)MessageType.FromMaster);

						request.Wait();

						var temp = new double[rows][];

						for (var i = 0; i < rows; i++)
						{
							temp[i] = matrixA.Data[offsetRow];
						}

						request = comm.ImmediateSend(temp, destination, (int)MessageType.FromMaster);
						request.Wait();

						var b = matrixB.Data;

						request = comm.ImmediateSend(b, destination, (int)MessageType.FromMaster);
						request.Wait();

						offsetRow = offsetRow + rows;
					}

					for (var source = 1; source <= numberOfSlaves; source++)
					{
						var offsetRowRequest = comm.ImmediateReceive<int>(source, (int)MessageType.FromSlave);
						offsetRowRequest.Wait();
						var rows = comm.ImmediateReceive<int>(source, (int)MessageType.FromSlave);
						rows.Wait();
						var temp = comm.ImmediateReceive<double[][]>(source, (int)MessageType.FromSlave);
						temp.Wait();

						//Console.Write("Recive from {0}-worker {1} rows. Time:{2}\n", source, rows, DateTime.Now.ToString("mm:ss.ffff"));

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
					var offsetRow = comm.ImmediateReceive<int>(source, (int)MessageType.FromMaster);
					offsetRow.Wait();
					var rows = comm.ImmediateReceive<int>(source, (int)MessageType.FromMaster);
					rows.Wait();
					var a = comm.ImmediateReceive<double[][]>(source, (int)MessageType.FromMaster);
					a.Wait();
					var b = comm.ImmediateReceive<double[][]>(source, (int)MessageType.FromMaster);
					b.Wait();

					//Console.Write("{0}-worker. Recive from master {1} rows. Time:{2}\n", comm.Rank, rows, DateTime.Now.ToString("mm:ss.ffff"));
					var c = new double[(int)rows.GetValue()][];

					for (var i = 0; i < (int)rows.GetValue(); i++)
					{
						c[i] = new double[columnA];
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

					var request = comm.ImmediateSend((int)offsetRow.GetValue(), source, (int)MessageType.FromSlave);
					request.Wait();
					request = comm.ImmediateSend((int)rows.GetValue(), source, (int)MessageType.FromSlave);
					request.Wait();
					request = comm.ImmediateSend(c, source, (int)MessageType.FromSlave);
					request.Wait();
					//Console.Write("{0}-worker. Send to master {1} rows. Time:{2}\n", comm.Rank, rows, DateTime.Now.ToString("mm:ss.ffff"));
				}
			}
		}
	}
}
