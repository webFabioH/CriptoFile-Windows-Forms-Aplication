using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Security.Cryptography;
using System.Runtime.InteropServices.WindowsRuntime;

namespace CriptoFile
{
    internal class Criptografia
    {
        //Declaração do CspParameter e RsaCryptoServiceProvider
        //Objetos com escopo global na classe
        public static CspParameters cspp;
        public static RSACryptoServiceProvider rsa;

        //Caminhos e variáveis para a fonte, pasta de criptografia,
        //e pasta de descriptografia
        private static string _encrFolder;

        public static string Encrfolder
        {
            get { return _encrFolder; }
            set 
            { 
                _encrFolder = value;
                PubKeyFile = _encrFolder + "rsaPublicKey.txt";
            }
        }

        public static string DecrFolder { get; set; }
        public static string SrcFolder { get; set; }

        //Arquivo de chave pública
        private static string PubKeyFile = Encrfolder + "rsaPublicKey.txt";

        //Chave contendo o nome para private/public key value pair
        public static string keyName;

        //Metodo para criar a chave pública
        public static string CreateAsmKeys()
        {
            string result = "";

            //Armazena uma key pair na key container
            if (string.IsNullOrEmpty(keyName))
            {
                result = "Chave pública não definida";
                return result;
            }

            cspp.KeyContainerName = keyName;
            rsa = new RSACryptoServiceProvider(cspp);
            rsa.PersistKeyInCsp = true;

            if (rsa.PublicOnly)
            {
                result = "Key: " + cspp.KeyContainerName + " - Somente Pública";
            }
            else
            {
                result = "Key: " + cspp.KeyContainerName + " - Key Pair Completa";
            }

            return result;
        }
    
        //Metodo para exportar a chave pública a em um arquivo
        public static bool ExportPublicKey()
        {
            bool result = true;

            if (rsa == null)
            {
                return false;
            }

            if (!Directory.Exists(Encrfolder))
            {
                Directory.CreateDirectory(Encrfolder); 
            }

            StreamWriter sw = new StreamWriter(PubKeyFile, false);
            try
            {
                sw.Write(rsa.ToXmlString(false));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                result = false;
            }
            finally 
            {
                sw.Close();
            }

            return result;
        }
    
        //Metodo para importar a chave pública de um arquivo
        public static string ImportPublickey()
        {
            string result = "";

            if (!File.Exists(PubKeyFile))
            {
                result = "Arquivo de chave pública não encontrado";
                return result;
            }

            if (string.IsNullOrEmpty(keyName))
            {
                result = "Chave pública não definida";
                return result;
            }

            StringReader sr = new StringReader(PubKeyFile);

            try
            {
                cspp.KeyContainerName = keyName;
                rsa = new RSACryptoServiceProvider(cspp);
                string keytxt = sr.ReadToEnd();
                rsa.FromXmlString(keytxt);
                rsa.PersistKeyInCsp = true;

                if (rsa.PublicOnly)
                {
                    result = "Key: " + cspp.KeyContainerName + " - Somente Pública";
                }
                else
                {
                    result = "Key: " + cspp.KeyContainerName + " - Key Pair Completa";
                }
            }
            catch (Exception ex) 
            { 
                result = ex.Message;
                Console.WriteLine(ex.Message);
            }
            finally
            {
                sr.Close();
            }

            return result;
        }
    
        //Metodo para criar chave privada a partir de um valor definido
        public static string GetPrivateKey()
        {
            string result = "";

            if (string.IsNullOrEmpty(keyName))
            {
                result = "Chave privada não definida";
                return result;
            }

            cspp.KeyContainerName = keyName;
            rsa = new RSACryptoServiceProvider(cspp);
            rsa.PersistKeyInCsp = true;

            if (rsa.PublicOnly)
            {
                result = "Key: " + cspp.KeyContainerName + " - Somente Pública";
            }
            else
            {
                result = "Key: " + cspp.KeyContainerName + " - Key Pair Completa";
            }

            return result;
        }
    
        //Metodo para criptografar arquivo
        public static string EncryptFile(string inFile)
        {
            //Criar uma instancia de Aes para criptografia simétrica dos dados
            Aes aes = Aes.Create();
            ICryptoTransform transform = aes.CreateEncryptor();

            //Usar o RSACryptoServiceProvider para criptografar a chave AES
            //O rsa é instaciado anteriormente: rsa = new RSACryptoServiceProvide(cspp)
            byte[] keyEncrypted = rsa.Encrypt(aes.Key, false);

            //Criar matrizes de bytes para conter os valores do comprimento da chave e IV.
            byte[] lenK = new byte[4];
            byte[] lenIV = new byte[4];

            int lKey = keyEncrypted.Length;
            lenK = BitConverter.GetBytes(lKey);
            int lIV = aes.IV.Length;
            lenIV = BitConverter.GetBytes(lIV);

            //Escrever no FileStream para arquivo criptografado
            // - comprimento de chave
            // - comprimento do IV
            // - chave criptografada
            // - O IV
            // - conteúdo da cifra criptografada

            int startFileName = inFile.LastIndexOf("\\") + 1;
            string outFile = Encrfolder + inFile.Substring(startFileName) + ".enc";

            try
            {
                using (FileStream outfs = new FileStream(outFile, FileMode.Create))
                {
                    outfs.Write(lenK, 0, 4);
                    outfs.Write(lenIV, 0, 4);
                    outfs.Write(keyEncrypted, 0, lKey);
                    outfs.Write(aes.IV, 0, lIV);

                    //Escreve o texto cifrado usando o CryptoStream para criptografar
                    using (CryptoStream outStreamEncrypted = new CryptoStream(outfs, transform, CryptoStreamMode.Write))
                    {
                        //Criptografando em partes é possível economizar memória
                        int count = 0;
                        int offset = 0;

                        //blockSizeBytes pode ter qualquer tamanho arbitrário
                        int blockSizeBytes = aes.BlockSize / 8;
                        byte[] data = new byte[blockSizeBytes];
                        int byteRead = 0;

                        using (FileStream infs = new FileStream(inFile, FileMode.Open))
                        {
                            do
                            {
                                count = infs.Read(data, 0, blockSizeBytes);
                                offset += count;
                                outStreamEncrypted.Write(data, 0, count);
                                byteRead += blockSizeBytes;
                            } while (count > 0);

                            infs.Close();
                        }

                        outStreamEncrypted.FlushFinalBlock();
                        outStreamEncrypted.Close();
                    }

                    outfs.Close();
                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }

            return $"Arquivo criptografado.\n" + 
                $"Origem: {inFile}\n" + 
                $"Destino: {outFile}"; 
        }

        //Metodo para descriptografar arquivo
        public static string DecryptFile(string inFile)
        {
            //Criar instância de Aes para descriptografia de arquivos
            Aes aes = Aes.Create();

            //Criar matrizes de bytes para obter o comprimento da chave e do IV
            //Os valores foram armazenados com 4 bytes
            byte[] LenK = new byte[4];
            byte[] LenIV = new byte[4];

            //Construir o nome do arquivo quando for descriptografado
            string outfile = DecrFolder + inFile.Substring(0, inFile.LastIndexOf("."));

            try
            {
                //Usa o filestream para ler algo criptografado e salvar em um outfs quando estiver descriptografado
                using (FileStream infs = new FileStream(Encrfolder + inFile, FileMode.Open))
                {
                    infs.Seek(0, SeekOrigin.Begin);
                    infs.Seek(0, SeekOrigin.Begin);
                    infs.Read(LenK, 0, 3);
                    infs.Seek(4, SeekOrigin.Begin);
                    infs.Read(LenIV, 0, 3);

                    //Converter os comprimentos para inteiros
                    int lenK = BitConverter.ToInt32(LenK, 0);
                    int lenIV = BitConverter.ToInt32(LenIV, 0);

                    //Determinar a posição inicial do texto cifrado e seu comprimeto
                    int startC = lenK + lenIV + 8;
                    int lenC = (int)infs.Length - startC;

                    //Criar matrizes para a chave Aes criptografada o IV e o texto cifrado
                    byte[] KeyEncrypted = new byte[lenK];
                    byte[] IV = new byte[lenIV];

                    //Extrair a chave IV a partir do indice 8 após os valores de comprimento
                    infs.Seek(8, SeekOrigin.Begin);
                    infs.Read(KeyEncrypted, 0, lenK);
                    infs.Seek(8 + lenK, SeekOrigin.Begin);
                    infs.Read(IV, 0, lenIV);

                    if (!Directory.Exists(DecrFolder))
                    {
                        Directory.CreateDirectory(DecrFolder);
                    }

                    //Uso o RSACryptoServiceProvider para descriptografar a chase AES
                    byte[] KeyDecrypted = rsa.Decrypt(KeyEncrypted, false);

                    //Descritografe a chave
                    ICryptoTransform transform = aes.CreateDecryptor(KeyDecrypted, IV);

                    //Descriptografar o texto do FileStream 
                    using (FileStream outfs = new FileStream(outfile, FileMode.Create))
                    {
                        int count = 0;
                        int offset = 0;

                        int blockSizeBytes = aes.BlockSize / 8;
                        byte[] data = new byte[blockSizeBytes];

                        //descriptografar em partes economiza memória e acomoda arquivos grandes
                        infs.Seek(startC, SeekOrigin.Begin);
                        using (CryptoStream outStremDecrypted = new CryptoStream(outfs, transform, CryptoStreamMode.Write))
                        {
                            do
                            {
                                count = infs.Read(data, offset, blockSizeBytes);
                                offset += count;
                                outStremDecrypted.Write(data, 0, count);
                            } while (count > 0);

                            outStremDecrypted.FlushFinalBlock();
                            outStremDecrypted.Close();
                        }

                        outfs.Close();
                    }
                    
                    infs.Close();
                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }

            return $"Arquivo descriptografado.\nOrigem: {inFile}\nDestino: {outfile}";
        }
    }
}
