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
				Console.Write("Rank ={0}. Time:{1}\n", comm.Rank, DateTime.Now.ToString("mm:ss.ffff"));

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

						Console.Write("Send to {0}-worker {1} rows. Time:{2}\n", destination, rows, DateTime.Now.ToString("mm:ss.ffff"));

						comm.Send(offsetRow, destination, (int)MessageType.FromMaster);
						comm.Send(rows, destination, (int)MessageType.FromMaster);

						var temp = new double[rows][];

						for (var i = 0; i < rows; i++)
						{
							temp[i] = matrixA.Data[offsetRow];
						}

						comm.Send(temp, destination, (int)MessageType.FromMaster);

						var b = matrixB.Data;

						comm.Send(b, destination, (int)MessageType.FromMaster);

						offsetRow = offsetRow + rows;
					}

					for (var source = 1; source <= numberOfSlaves; source++)
					{
						offsetRow = comm.Receive<int>(source, (int)MessageType.FromSlave);
						var rows = comm.Receive<int>(source, (int)MessageType.FromSlave);
						var temp = comm.Receive<double[][]>(source, (int)MessageType.FromSlave);

						Console.Write("Recive from {0}-worker {1} rows. Time:{2}\n", source, rows, DateTime.Now.ToString("mm:ss.ffff"));

						for (var i = 0; i < rows; i++)
						{
							matrixC.Data[offsetRow + i] = temp[i];
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
					var offsetRow = comm.Receive<int>(source, (int)MessageType.FromMaster);
					var rows = comm.Receive<int>(source, (int)MessageType.FromMaster);
					var a = comm.Receive<double[][]>(source, (int)MessageType.FromMaster);
					var b = comm.Receive<double[][]>(source, (int)MessageType.FromMaster);

					Console.Write("{0}-worker. Recive from master {1} rows. Time:{2}\n", comm.Rank, rows, DateTime.Now.ToString("mm:ss.ffff"));
					var c = new double[rows][];

					for (var i = 0; i < rows; i++)
					{
						c[i] = new double[columnA];
					}

					for (var k = 0; k < columnB; k++)
					{
						for (var i = 0; i < rows; i++)
						{
							c[i][k] = 0.0;
							for (var j = 0; j < columnA; j++)
							{
								c[i][k] = c[i][k] + a[i][j]*b[j][k];
							}
						}
					}

					comm.Send(offsetRow, source, (int)MessageType.FromSlave);
					comm.Send(rows, source, (int)MessageType.FromSlave);
					comm.Send(c, source, (int)MessageType.FromSlave);
					Console.Write("{0}-worker. Send to master {1} rows. Time:{2}\n", comm.Rank, rows, DateTime.Now.ToString("mm:ss.ffff"));
				}
			}
		}
	}
}
