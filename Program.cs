using System;
using System.IO;
using System.Net.Mail;

namespace SmartMail
{
    class Program
    {
        static private String Version = new String(@"V1.0");
        static private String nomFichierListeMails = new String(@"ListeMails.txt");
        static private String nomFichierAlerteMail = new String(@"AlerteMail.txt");
        static private String nomFichierLog = new String(@"log.txt");
        static private String nomRepertoireDefault = new String(@"DefaultDir");
        static private String domaine = new String(@"domaine");
        static private SmtpClient SmtpServer = new SmtpClient();
        static private MailAddress noReply;
        static private void ReadGeneralConfig()
        {
            try
            {
                StreamReader generalconfigfile = new StreamReader(@"config.ini");
                string line;
                int cpt = 0;
                while ((line = generalconfigfile.ReadLine()) != null)
                {
                    if (line.Contains("inputDir"))
                    {
                        cpt++;
                        nomRepertoireDefault = line.Substring(line.IndexOf("=") + 1);
                        if(nomRepertoireDefault.Length > 0)
                        {
                            nomFichierListeMails = nomRepertoireDefault + "\\" + "ListeMails.txt";
                            nomFichierAlerteMail = nomRepertoireDefault + "\\" + "AlerteMail.txt";
                            nomFichierLog = nomRepertoireDefault + "\\" + "Log.txt";
                        }
                    }
                    if (line.Contains("mailServer"))
                    {
                        cpt++;
                        SmtpServer.Host = line.Substring(line.IndexOf("=") + 1);
                    }
                    if (line.Contains("noReply"))
                    {
                        cpt++;
                        noReply = new MailAddress(line.Substring(line.IndexOf("=") + 1));
                    }
                    if (line.Contains("domainName"))
                    {
                        cpt++;
                        domaine = line.Substring(line.IndexOf("=") + 1);
                    }
                }
                generalconfigfile.Close();
                if(cpt == 4)
                {
                    LectureFichierMail();
                }
                else
                {
                    Console.WriteLine("\n\nLe fichier ini doit contenir un tag [global] avec 4 entrées : inputDir, mailServer, noReply et domainName.\nElles doivent être suivies d'un '=' puis de la valeur (sans espaces après le '=' et sans guillemets)");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        static private void ajouteLog(String chaine)
        {
            try
            {
                string chaineDatee = DateTime.Now.ToString("yyyy-MM-dd--HH-mm-ss") + " : \t" + chaine + "\n";
                if (File.Exists(nomFichierLog))
                {
                    FileInfo fi = new FileInfo(nomFichierLog);
                    long len = fi.Length;
                    if (len > 10000000)
                    {
                        // Si fichier supérieur à 10 MO, création d'un nouveau fichier
                        string nomFichierLogOld = nomRepertoireDefault + "\\Logs_old.txt";
                        // If file already exists
                        if (File.Exists(nomFichierLogOld))
                        {
                            File.Delete(nomFichierLogOld);
                        }
                        File.Move(nomFichierLog, nomFichierLogOld);
                        File.Create(nomFichierLog).Close(); // Create file
                    }
                    using (StreamWriter sw = File.AppendText(nomFichierLog))
                    {
                        sw.WriteLine(chaineDatee);
                        sw.Close();
                    }
                }
                else
                {
                    // If file does not exists
                    File.Create(nomFichierLog).Close(); // Create file
                    using (StreamWriter sw = File.AppendText(nomFichierLog))
                    {
                        sw.WriteLine(chaineDatee);
                        sw.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        static private void LectureFichierMail()
        {
            string txtLog = new String("");
            try
            {
                int aFaire = 0;
                string nomFichierAlerteMailOld = new String(nomFichierAlerteMail + ".old");
                string text = "";
                string sujet = "";
                // Si fichier Alerte
                if (File.Exists(nomFichierAlerteMail))
                {
                    text = File.ReadAllText(nomFichierAlerteMail, System.Text.Encoding.GetEncoding("iso-8859-1"));
                    if (File.Exists(nomFichierAlerteMailOld))
                    {
                        string textOld = File.ReadAllText(nomFichierAlerteMailOld, System.Text.Encoding.GetEncoding("iso-8859-1"));
                        if(text != textOld)
                        {
                            aFaire = 1;
                        }
                    }
                    else
                    {
                        aFaire = 1;
                    }
                }
                if (aFaire == 1)
                {
                    StreamReader file = new StreamReader(nomFichierAlerteMail, System.Text.Encoding.GetEncoding("iso-8859-1"));
                    if ((sujet = file.ReadLine()) == null)
                    {
                        sujet = "Alerte";
                    }
                    file.Close();
                    // Lecture des adresses mails
                    if (File.Exists(nomFichierListeMails))
                    {
                        // Lecture du corps du mail
                        string[] lines = File.ReadAllLines(nomFichierListeMails);

                        MailMessage mail = new MailMessage();
                        mail.From = noReply;
                        mail.Subject = sujet;
                        mail.Body = text;

                        // serveur mail
                        SmtpServer.Port = 25;
                        SmtpServer.EnableSsl = false;
                        SmtpServer.UseDefaultCredentials = false;

                        txtLog = "Version = " + Version + "\n";
                        txtLog = txtLog + "Envoi d'un mail\nà : ";
                        foreach (string line in lines)
                        {
                            if (line.IndexOf("@") > 0)
                            {
                                if (line.Substring(line.IndexOf("@")) != domaine)
                                {
                                    txtLog = txtLog + line + " (supprimé car non conforme); ";
                                }
                                else
                                {
                                    mail.To.Add(line);
                                    txtLog = txtLog + line + "; ";
                                }
                            }
                            else
                            {
                                txtLog = txtLog + line + " (supprimé car non conforme); ";
                            }
                        }
                        txtLog = txtLog + "\nCorps : \n" + text;

                        ajouteLog(txtLog);
                        SmtpServer.Send(mail);
                        if (File.Exists(nomFichierAlerteMailOld))
                        {
                            File.Delete(nomFichierAlerteMailOld);
                        }
                        File.Move(nomFichierAlerteMail, nomFichierAlerteMailOld);
                    }
                    else
                    {
                        ajouteLog("Le fichier n'existe pas : " + nomFichierListeMails);
                    }
                }
            }
            catch (Exception ex)
            {
                ajouteLog(ex.ToString());
            }
        }
        static void Main(string[] args)
        {
            ReadGeneralConfig();
        }
    }
}
