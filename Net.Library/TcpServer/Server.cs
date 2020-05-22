using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace SomeProject.Library.Server
{
    /// <summary>
    /// Класс сервера для принятия файлов и текстовых сообщений
    /// </summary>
    public class Server
    {
        static string directory;
        static DateTime lastDay;
        static int currFileNamber;
        static int maxConnections = 3;
        static int currentConnections = 0;
        TcpListener serverListener;

        public Server()
        {
            serverListener = new TcpListener(IPAddress.Loopback, 8080);
        }

        /// <summary>
        /// Логирует завершение сервера
        /// </summary>
        /// <returns>Сервер завершен успешно?</returns>
        public bool TurnOffListener()
        {
            try
            {
                if (serverListener != null)
                    serverListener.Stop();
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Cannot turn off listener: " + e.Message);
                return false;
            }
        }

        /// <summary>
        /// Запуск сервера на прослушиваение порта 8080, каждый клиент обрабатывается отдельным потоком
        /// </summary>
        /// <returns></returns>
        public async Task TurnOnListener()
        {
            try
            {
                if (serverListener != null)
                    serverListener.Start();
                while (true)
                {
                    Console.WriteLine("Waiting for connections...");
                    TcpClient client = await serverListener.AcceptTcpClientAsync();
                    Interlocked.Increment(ref currentConnections);
                    if (currentConnections == maxConnections)
                    {
                        Console.WriteLine("Client " + currentConnections + " declined: Server is overloaded");
                        client.Close();
                        Interlocked.Decrement(ref currentConnections);
                        continue;
                    }
                    ReceivePacketFromClient(client);
                }
            }
            catch (Exception e)
            {
                Interlocked.Decrement(ref currentConnections);
                Console.WriteLine("Cannot turn on listener: " + e.Message);
            }
        }

        /// <summary>
        /// Асинхронно принимает пакет от клиента, определяет его тип и вызывает соответствующий обработчик
        /// </summary>
        /// <param name="client">клиент</param>
        public async Task ReceivePacketFromClient(TcpClient client)
        {
            try
            {

                StringBuilder recievedMessage = new StringBuilder();

                int clientNum = currentConnections;
                Console.WriteLine("Accepted connection: " + currentConnections);
                await Task.Delay(3000);
                byte[] data = new byte[1];
                NetworkStream stream = client.GetStream();

                do
                {
                    int bytes = stream.Read(data, 0, data.Length);
                    recievedMessage.Append(Encoding.UTF8.GetString(data, 0, bytes));
                    if (data[0] == 6)
                    {
                        break;
                    }
                } while (stream.DataAvailable);

                var header = recievedMessage.ToString().Substring(0, 15).Trim(new char[] { (char) 0 }).Split(',');

                OperationResult resp;
                if (header[0] == "text")
                {
                    resp = await ReceiveMessageFromClient(stream);
                }
                else if (header[0] == "file")
                {
                    resp = await ReceiveFileFromClient(stream, header[1]);
                }
                else
                {
                    resp = new OperationResult(Result.Fail, "Unknown packet type");
                }


                if (resp.Result == Result.OK)
                {
                    Console.WriteLine("New message from client " + clientNum + ": " + resp.Message);
                    await SendMessageToClient(stream, "Success");
                }
                else
                {
                    Console.WriteLine("Error: " + clientNum + ": " + resp.Message);
                    await SendMessageToClient(stream, resp.Message);
                }

                stream.Close();
                Interlocked.Decrement(ref currentConnections);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
                Interlocked.Decrement(ref currentConnections);
            }
            finally
            {
                client.Close();
            }
        }

        /// <summary>
        /// Асинхронно принимает текстовое сообщение
        /// </summary>
        /// <param name="stream">Сетевой поток клиента</param>
        /// <returns>Результат операции</returns>
        public async Task<OperationResult> ReceiveMessageFromClient(NetworkStream stream)
        {
            try
            {
                StringBuilder recievedMessage = new StringBuilder();

                byte[] data = new byte[256];
                do
                {
                    int bytes = await stream.ReadAsync(data, 0, data.Length);
                    recievedMessage.Append(Encoding.UTF8.GetString(data, 0, bytes));
                }
                while (stream.DataAvailable);

                return new OperationResult(Result.OK, recievedMessage.ToString());
            }
            catch (Exception e)
            {
                return new OperationResult(Result.Fail, e.Message);
            }
        }

        /// <summary>
        /// Асинхронно принимает файл от клиента
        /// </summary>
        /// <param name="stream">Сетевой поток клиента</param>
        /// <param name="extention">Расширение файла</param>
        public async Task<OperationResult> ReceiveFileFromClient(NetworkStream stream, string extention)
        {
            try
            {
                if (DateTime.Today != lastDay)
                {
                    lastDay = DateTime.Today;
                    directory = DateTime.Today.ToString("yyyy-MM-dd");
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                    else
                    {
                        currFileNamber = Directory.GetFiles(directory).Length;
                    }
                }
                var fileNumber = Interlocked.Increment(ref currFileNamber);
                var fileName = "File" + fileNumber + "." + extention;
                var fileStream = File.Create(Path.Combine(directory, fileName));

                byte[] data = new byte[256];

                var file = new List<byte>();

                int offset = 0;
                do
                {
                    int bytes = stream.Read(data, 0, data.Length);
                    foreach (var b in data)
                    {
                        file.Add(b);
                    }

                    offset += bytes;
                }
                while (stream.DataAvailable);

                await fileStream.WriteAsync(file.ToArray(), 0, file.Count);

                fileStream.Close();

                return new OperationResult(Result.OK, "Recieved " + fileName);
            }
            catch (Exception e)
            {
                return new OperationResult(Result.Fail, e.Message);
            }
        }

        /// <summary>
        /// Отправляет сообщение клиенту
        /// </summary>
        /// <param name="message">сообщение</param>
        /// <returns></returns>
        public async Task<OperationResult> SendMessageToClient(NetworkStream stream, string message)
        {
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(message);
                await stream.WriteAsync(data, 0, data.Length);
                stream.Close();
                return new OperationResult(Result.OK, "");
            }
            catch (Exception e)
            {
                stream.Close();
                return new OperationResult(Result.Fail, e.Message);
            }
        }
    }
}