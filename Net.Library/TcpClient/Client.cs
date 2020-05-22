using System;
using System.Text;
using System.Net.Sockets;
using System.IO;

namespace SomeProject.Library.Client
{
    /// <summary>
    /// Класс клиента
    /// </summary>
    public class Client
    {
        public TcpClient tcpClient;

        /// <summary>
        /// Принимает сообщение от сервера
        /// </summary>
        /// <param name="stream">Сетевой поток</param>
        /// <returns>Результат</returns>
        private OperationResult ReceiveMessageFromServer(NetworkStream stream)
        {
            try
            {
                StringBuilder recievedMessage = new StringBuilder();
                byte[] data = new byte[256];

                do
                {
                    int bytes = stream.Read(data, 0, data.Length);
                    recievedMessage.Append(Encoding.UTF8.GetString(data, 0, bytes));
                }
                while (stream.DataAvailable);
                stream.Close();
                tcpClient.Close();

                if (recievedMessage.ToString() == "Success")
                {
                    return new OperationResult(Result.OK, recievedMessage.ToString());
                }
                else
                {
                    return new OperationResult(Result.Fail, recievedMessage.ToString());
                }
            }
            catch (IOException)
            {
                return new OperationResult(Result.Fail, "Fail");
            }
            catch (Exception e)
            {
                return new OperationResult(Result.Fail, e.ToString());
            }
        }

        /// <summary>
        /// Отправляет текстовое сообщение на сервер
        /// </summary>
        /// <param name="message">Текстовое сообщение</param>
        /// <returns>Результат</returns>
        public OperationResult SendMessageToServer(string message)
        {
            try
            {
                tcpClient = new TcpClient("127.0.0.1", 8080);
                NetworkStream stream = tcpClient.GetStream();
                var header = Encoding.UTF8.GetBytes("text");
                var data = Encoding.UTF8.GetBytes(message);
                byte[] packet = ConstructPacket(header, data);
                stream.Write(packet, 0, packet.Length);
                var result = ReceiveMessageFromServer(stream);
                stream.Close();
                tcpClient.Close();
                return result;
            }
            catch (IOException)
            {
                return new OperationResult(Result.Fail, "Fail");
            }
            catch (Exception e)
            {
                return new OperationResult(Result.Fail, e.Message);
            }
        }

        /// <summary>
        /// Отправляет файл на сервер
        /// </summary>
        /// <param name="filePath">Путь к файлу</param>
        /// <param name="extention">Разрешение файла</param>
        /// <returns>Результат</returns>
        public OperationResult SendFileToServer(string filePath, string extention)
        {
            try
            {
                tcpClient = new TcpClient("127.0.0.1", 8080);
                NetworkStream stream = tcpClient.GetStream();
                var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                var data = new byte[fileStream.Length];
                fileStream.Read(data, 0, (int)fileStream.Length);
                var header = Encoding.UTF8.GetBytes("file," + extention);
                byte[] packet = ConstructPacket(header, data);
                stream.Write(packet, 0, (packet.Length));
                fileStream.Close();
                var result = ReceiveMessageFromServer(stream);
                stream.Close();
                tcpClient.Close();
                return result;
            }
            catch (IOException)
            {
                return new OperationResult(Result.Fail, "fail");
            }
            catch (Exception e)
            {
                return new OperationResult(Result.Fail, e.Message);
            }
        }

        private byte[] ConstructPacket(byte[] head, byte[] body)
        {
            if (head.Length > 15) throw new FormatException("header is too big");
            byte[] packet = new byte[16 + body.Length];
            head.CopyTo(packet, 0);
            packet[15] = 6;
            body.CopyTo(packet, 16);
            return packet;
        } 
    }
}
