using System;
using MPI;

namespace MatrixMultiple
{
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;

	enum MessageType
	{
		FromMaster = 10,
		FromSlave = 20
	}

	class Program
	{
		static void Main(string[] args)
		{
			var groupsCount = int.Parse(args[0]);

			using (new MPI.Environment(ref args))
			{
				var world = Communicator.world;

				var remaingingProcess = world.Size;

				if (groupsCount > remaingingProcess / 2)
				{
					throw new Exception("Need more process or less group!");
				}

				var r = new Random();

				var currentRank = 0;

				var communicatrosList = new List<Communicator>(groupsCount);
				var groupRanksList = new List<int[]>(groupsCount);

				for (var i = 0; i < groupsCount; i++)
				{
					var groupInProcess = 2;
					
					if (i + 1 == groupsCount)
					{
						groupInProcess = remaingingProcess;
					}
					else
					{
						if (world.Rank == 0)
						{
							groupInProcess = r.Next(2, remaingingProcess - (groupsCount - i - 1) * 2);
						}

						world.Broadcast(ref groupInProcess, 0);
					}

					remaingingProcess -= groupInProcess;
					var temp = new int[groupInProcess];

					for (var j = 0; j < groupInProcess; j++)
					{
						temp[j] = currentRank++;
					}

					groupRanksList.Add(temp);
					var newGroup = world.Group.IncludeOnly(temp);
					communicatrosList.Add(world.Create(newGroup));
				}

				for (var i = 0; i < groupsCount; i++)
				{
					if (groupRanksList[i].Contains(world.Rank))
					{
						MPIMatrixMultiple(communicatrosList[i], i.ToString());
					}
				}
			}
		}

		static void MPIMatrixMultiple(Communicator comm, string groupName)
		{
			var fileOffset = 2 * sizeof(int);
			var matrixManager = new MatrixManager();
			var matrixA = matrixManager.FileInitialization("A");
			var matrixB = matrixManager.FileInitialization("B");

			//var matrixA = new Matrix(11, 2);
			//var matrixB = new Matrix(2, 4);
			//Console.WriteLine("{0} {1} {2}", matrixA.Rows, matrixA.Columns, matrixB.Columns);
			//matrixManager.SimpleInitialization(matrixA);
			//matrixManager.SimpleInitialization(matrixB);

			var rowsA = matrixA.Rows;
			var columnA = matrixA.Columns;
			var columnB = matrixB.Columns;

			if (comm.Rank == 0)
			{
				int numberOfSlaves = comm.Size - 1;
				var start = DateTime.Now;

				int rowsPerSlave = rowsA / numberOfSlaves;
				int remainingRows = rowsA % numberOfSlaves;
				int offsetRow = 0;

				var b = matrixB.Data;

				((Intracommunicator)comm).Broadcast(ref b, 0);

				for (var destination = 1; destination <= numberOfSlaves; destination++)
				{
					int rows = (destination <= remainingRows) ? rowsPerSlave + 1 : rowsPerSlave;

					comm.Send(offsetRow, destination, (int)MessageType.FromMaster);
					comm.Send(rows, destination, (int)MessageType.FromMaster);

					var temp = new double[rows][];

					for (var i = 0; i < rows; i++)
					{
						temp[i] = matrixA.Data[offsetRow];
					}

					comm.Send(temp, destination, (int)MessageType.FromMaster);
					offsetRow = offsetRow + rows;
				}


				using (var fs = new FileStream(groupName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write))
				{
					byte[] buff = BitConverter.GetBytes(rowsA);
					fs.Write(buff, 0, sizeof(int));

					buff = BitConverter.GetBytes(columnB);
					fs.Write(buff, 0, sizeof(int));
				}

				comm.Barrier();

				DateTime end = DateTime.Now;
				Console.WriteLine("Group name: {0} with size = {1}", groupName, comm.Size);
				Console.Write("\n\n");
				Console.Write(end - start);
			}
			else
			{
				double[][] b = null;

				((Intracommunicator)comm).Broadcast<double[][]>(ref b, 0);

				const int source = 0;
				var offsetRow = comm.Receive<int>(source, (int)MessageType.FromMaster);
				var rows = comm.Receive<int>(source, (int)MessageType.FromMaster);
				var a = comm.Receive<double[][]>(source, (int)MessageType.FromMaster);

				var c = new double[rows][];

				for (var i = 0; i < rows; i++)
				{
					c[i] = new double[columnB];
				}

				//Console.WriteLine("{0} {1} {2}", columnB, rows, columnA);

				using (var fs = new FileStream(groupName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write))
				{
					fs.Seek(sizeof (double)*offsetRow*columnB + fileOffset, SeekOrigin.Begin);
					for (var k = 0; k < columnB; k++)
					{
						for (var i = 0; i < rows; i++)
						{
							c[i][k] = 0.0;
							for (var j = 0; j < columnA; j++)
							{
								c[i][k] = c[i][k] + a[i][j] * b[j][k];
							}

							//Console.WriteLine("{0} {1:0.###} {2} {3} {4} {5}", comm.Rank, c[i][k], sizeof(double), offsetRow, i, k);
							byte[] buff = BitConverter.GetBytes(c[i][k]);
							fs.Write(buff, 0, sizeof(double));
						}
					}
				}

				comm.Barrier();
			}
		}
	}
}
