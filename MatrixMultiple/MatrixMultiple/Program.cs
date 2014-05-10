using System;
using MPI;

namespace MatrixMultiple
{
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Text;
	using System.Threading;

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
			var rowsA = 5;
			var columnA = 5;
			var columnB = 5;

			Console.WriteLine("Rank = {0}",comm.Rank);

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

					for (var i = 0; i < rows; i++)
					{
						matrixC.Data[offsetRow + i] = temp[i];
					}
				}

				DateTime end = DateTime.Now;
				Console.WriteLine("Group name: {0} with size = {1}", groupName, comm.Size);
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

				var c = new double[rows][];

				for (var i = 0; i < rows; i++)
				{
					c[i] = new double[columnB];
				}

				for (var k = 0; k < columnB; k++)
				{
					for (var i = 0; i < rows; i++)
					{
						c[i][k] = 0.0;
						for (var j = 0; j < columnA; j++)
						{
							c[i][k] = c[i][k] + a[i][j] * b[j][k];
						}
					}
				}

				comm.Send(offsetRow, source, (int)MessageType.FromSlave);
				comm.Send(rows, source, (int)MessageType.FromSlave);
				comm.Send(c, source, (int)MessageType.FromSlave);
			}
		}
	}
}
