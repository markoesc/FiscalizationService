using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.Text;
using System.Windows.Forms;
using System.Xml;

namespace MarkoEsc.FiscalizationService
{
    public partial class frmMain : Form
    {
        /*public string IssuerTIN = "12345678";
        public string BusinUnitCode = "xx123xx123";
        public string SoftCode = "ss123ss123";
        public string MaintainerCode = "mm123mm123";
        public string OperatorCode = "oo123oo123";*/

        private X509Certificate2 Certificate { get; set; }
        private CIS.FiscalizationServicePortTypeClient Client { get; set; }
        private string Path { get; set; }

        public frmMain()
        {
            InitializeComponent();

            settingsBindingSource.DataSource = Properties.Settings.Default;

            Certificate = new X509Certificate2("CoreitPecatSoft.pfx", "123456", X509KeyStorageFlags.Exportable);

            Client = new CIS.FiscalizationServicePortTypeClient(new BasicHttpsBinding(BasicHttpsSecurityMode.Transport), new EndpointAddress(Properties.Settings.Default.FiscalizationService));

            Path = Directory.CreateDirectory("Logs").FullName;
        }

        private void PrintToScreen(XmlDocument xml)
        {
            using (var MS = new MemoryStream())
            {
                xml.Save(MS);
                textBox2.Text = Encoding.UTF8.GetString(MS.ToArray());
            }
        }

        private void saveConfiguration_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.Save();

            Client = new CIS.FiscalizationServicePortTypeClient(new BasicHttpsBinding(BasicHttpsSecurityMode.Transport), new EndpointAddress(Properties.Settings.Default.FiscalizationService));
        }

        private void registerTCR_Click(object sender, EventArgs e)
        {
            var request = new CIS.RegisterTCRRequest
            {
                Header = new CIS.RegisterTCRRequestHeaderType
                {
                    SendDateTime = Helper.RoundDateTime(DateTime.Now),
                    UUID = Guid.NewGuid().ToString()
                },
                TCR = new CIS.TCRType
                {
                    IssuerTIN = Properties.Settings.Default.IssuerTIN,
                    BusinUnitCode = Properties.Settings.Default.BusinUnitCode,
                    TCRIntID = Properties.Settings.Default.TCRIntId,
                    SoftCode = Properties.Settings.Default.SoftCode,
                    MaintainerCode = Properties.Settings.Default.MaintainerCode,
                    Type = CIS.TCRSType.REGULAR,
                    TypeSpecified = true
                }
            };

            Helper.SignRequest(request, Certificate);

            Helper.ConvertToXml(request).Save(String.Format(@"{0}\{1:yyyyMMdd_hhmmss}_{2}_Request.xml", Path, request.Header.SendDateTime, request.Header.UUID));

            try
            {
                var response = Client.registerTCR(request);

                var xml = Helper.ConvertToXml(response);
                xml.Save(String.Format(@"{0}\{1:yyyyMMdd_hhmmss}_{2}_Response.xml", Path, request.Header.SendDateTime, request.Header.UUID));

                PrintToScreen(xml);

                Properties.Settings.Default.TCRCode = tCRCodeTextBox.Text = response.TCRCode;
                Properties.Settings.Default.Save();
            }
            catch (FaultException err)
            {
                var xml = XmlWriter.Create(String.Format(@"{0}\{1:yyyyMMdd_hhmmss}_{2}_Response.xml", Path, request.Header.SendDateTime, request.Header.UUID), new XmlWriterSettings { Indent = true });
                err.CreateMessageFault().WriteTo(xml, EnvelopeVersion.Soap11);
                xml.Flush();
                xml.Dispose();

                var fault = err.CreateMessageFault();
                var code = fault.GetReaderAtDetailContents().ReadElementContentAsInt();

                MessageBox.Show(String.Format("Error code: {0}\n{1}", code.ToString(), err.Message), "Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
        }

        private void registerCashDeposit_Click(object sender, EventArgs e)
        {
            var request = new CIS.RegisterCashDepositRequest
            {
                Header = new CIS.RegisterCashDepositRequestHeaderType
                {
                    SendDateTime = Helper.RoundDateTime(DateTime.Now),
                    UUID = Guid.NewGuid().ToString()
                },
                CashDeposit = new CIS.CashDepositType
                {
                    ChangeDateTime = Helper.RoundDateTime(DateTime.Now),
                    Operation = ((Button)sender).Tag.Equals("INITIAL") ? CIS.CashDepositOperationSType.INITIAL : CIS.CashDepositOperationSType.WITHDRAW,
                    CashAmt = decimal.Parse(textBox1.Text),
                    TCRCode = Properties.Settings.Default.TCRCode,
                    IssuerTIN = Properties.Settings.Default.IssuerTIN
                }
            };

            Helper.SignRequest(request, Certificate);

            Helper.ConvertToXml(request).Save(String.Format(@"{0}\{1:yyyyMMdd_hhmmss}_{2}_Request.xml", Path, request.Header.SendDateTime, request.Header.UUID));

            try
            {
                var response = Client.registerCashDeposit(request);

                var xml = Helper.ConvertToXml(response);
                xml.Save(String.Format(@"{0}\{1:yyyyMMdd_hhmmss}_{2}_Response.xml", Path, request.Header.SendDateTime, request.Header.UUID));

                PrintToScreen(xml);
            }
            catch (FaultException err)
            {
                var xml = XmlWriter.Create(String.Format(@"{0}\{1:yyyyMMdd_hhmmss}_{2}_Response.xml", Path, request.Header.SendDateTime, request.Header.UUID), new XmlWriterSettings { Indent = true });
                err.CreateMessageFault().WriteTo(xml, EnvelopeVersion.Soap11);
                xml.Flush();
                xml.Dispose();

                var fault = err.CreateMessageFault();
                var code = fault.GetReaderAtDetailContents().ReadElementContentAsInt();

                MessageBox.Show(String.Format("Error code: {0}\n{1}", code.ToString(), err.Message), "Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            var request = new CIS.RegisterInvoiceRequest
            {
                Header = new CIS.RegisterInvoiceRequestHeaderType
                {
                    SendDateTime = Helper.RoundDateTime(DateTime.Now),
                    UUID = Guid.NewGuid().ToString()
                },
                Invoice = new CIS.InvoiceType
                {
                    TypeOfInv = CIS.InvoiceSType.CASH,
                    IssueDateTime = Helper.RoundDateTime(DateTime.Now),
                    IsIssuerInVAT = true,
                    TCRCode = Properties.Settings.Default.TCRCode,
                    InvOrdNum = (int)numericUpDown1.Value,
                    InvNum = String.Format("{0}/{1}/{2}", (int)numericUpDown1.Value, DateTime.Now.Year, Properties.Settings.Default.TCRCode),
                    SoftCode = Properties.Settings.Default.SoftCode,
                    TotPriceWoVAT = 10.00m,
                    TotPrice = 12.10m,
                    OperatorCode = Properties.Settings.Default.OperatorCode,
                    BusinUnitCode = Properties.Settings.Default.BusinUnitCode,
                    PayMethods = new CIS.PayMethodType[]
                    {
                        new CIS.PayMethodType
                        {
                            Type = CIS.PaymentMethodTypeSType.BANKNOTE,
                            Amt = 12.10m,
                        }
                    },
                    Seller = new CIS.SellerType
                    {
                        IDType = CIS.IDTypeSType.TIN,
                        IDNum = Properties.Settings.Default.IssuerTIN,
                        Name = "Test d.o.o",
                    },
                    Items = new CIS.InvoiceItemType[]
                    {
                        new CIS.InvoiceItemType
                        {
                            N = "Test artikal",
                            C = "1234234",
                            U = "kom",
                            Q = 10.0,
                            UPB = 1.21m,
                            UPA = 1.21m,
                            PB = 10.00m,
                            PA = 12.10m,
                            VR = 21.00m,
                            VRSpecified = true,
                            VA = 2.10m,
                            VASpecified = true,
                        }
                    }
                }
            };

            var IKOF = Helper.ComputeIIC(request.Invoice.Seller.IDNum, request.Invoice.IssueDateTime, request.Invoice.InvOrdNum, request.Invoice.BusinUnitCode, request.Invoice.TCRCode, request.Invoice.SoftCode, request.Invoice.TotPrice, Certificate);
            request.Invoice.IIC = IKOF.IIC;
            request.Invoice.IICSignature = IKOF.IICSignature;

            Helper.SignRequest(request, Certificate);

            Helper.ConvertToXml(request).Save(String.Format(@"{0}\{1:yyyyMMdd_hhmmss}_{2}_Request.xml", Path, request.Header.SendDateTime, request.Header.UUID));

            try
            {
                var response = Client.registerInvoice(request);

                var xml = Helper.ConvertToXml(response);
                xml.Save(String.Format(@"{0}\{1:yyyyMMdd_hhmmss}_{2}_Response.xml", Path, request.Header.SendDateTime, request.Header.UUID));

                PrintToScreen(xml);

                textBox3.Text = response.FIC;
            }
            catch (FaultException err)
            {
                var xml = XmlWriter.Create(String.Format(@"{0}\{1:yyyyMMdd_hhmmss}_{2}_Response.xml", Path, request.Header.SendDateTime, request.Header.UUID), new XmlWriterSettings { Indent = true });
                err.CreateMessageFault().WriteTo(xml, EnvelopeVersion.Soap11);
                xml.Flush();
                xml.Dispose();

                var fault = err.CreateMessageFault();
                var code = fault.GetReaderAtDetailContents().ReadElementContentAsInt();

                MessageBox.Show(String.Format("Error code: {0}\n{1}", code.ToString(), err.Message), "Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
        }
    }
}