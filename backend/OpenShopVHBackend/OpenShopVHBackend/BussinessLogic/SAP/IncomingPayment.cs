﻿using OpenShopVHBackend.Models;
using SAPbobsCOM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using System.Data.Entity;

namespace OpenShopVHBackend.BussinessLogic.SAP
{
    public class IncomingPayment
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        private ServerConnection _connection;

        public IncomingPayment(){
            _connection = new ServerConnection();
        }

        private ICompany company { get; set; }

        public String MakePayment(int paymentId, int userId)
        {

            String lastMessage = "";

            var user = db.DeviceUser.Where(w => w.DeviceUserId == userId).ToList().FirstOrDefault();
            String connection = user.Shop == null ? "" : user.Shop.ConnectionString;

            var p = db.Payments
                    .Include(i => i.Cash).Include(i => i.Transfer).Include(i => i.Checks).Include(i => i.Invoices)
                    .ToList()
                    .Where(w => w.PaymentId == paymentId)
                    .FirstOrDefault();

            using (var db = new ApplicationDbContext(connection))
            {
                if (p != null)
                {
                    if (_connection.Connect(connection) == 0)
                    {
                        company = _connection.GetCompany();

                        Double totalAmount = p.TotalAmount;
                        Double amountLeft = totalAmount;

                        Payments payment = company.GetBusinessObject(BoObjectTypes.oIncomingPayments);
                        payment.DocType = BoRcptTypes.rCustomer;
                        //payment.DocTypte = BoRcptTypes.rCustomer;
                        payment.CardCode = p.Client.CardCode;
                        payment.DocDate = DateTime.Now;
                        payment.VatDate = DateTime.Now;
                        payment.DueDate = DateTime.Now;

                        if (p.Cash != null)
                        {
                            payment.CashAccount = p.Cash.GeneralAccount;
                            payment.CashSum = p.Cash.Amount;
                        }

                        if (p.Transfer != null)
                        {
                            payment.TransferAccount = p.Transfer.GeneralAccount;
                            payment.TransferDate = p.Transfer.Date;
                            payment.TransferReference = p.Transfer.ReferenceNumber;
                            payment.TransferSum = p.Transfer.Amount;
                        }

                        if (p.Checks != null)
                        {
                            foreach (Check check in p.Checks)
                            {
                                payment.Checks.CheckAccount = check.GeneralAccount;
                                payment.Checks.CheckSum = check.Amount;
                                payment.Checks.DueDate = check.DueDate;
                                payment.Checks.BankCode = check.Bank.FormatCode;
                                payment.Checks.Add();
                            }
                        }

                        if (p.Invoices != null)
                        {
                            var invoices = p.Invoices
                            .Where(w => w.PaymentId == p.PaymentId)
                            .ToList();

                            foreach (InvoiceItem invoice in invoices)
                            {
                                if (amountLeft > 0)
                                {
                                    payment.Invoices.DocEntry = invoice.DocEntry;
                                    payment.Invoices.InvoiceType = BoRcptInvTypes.it_Invoice;
                                    //Si aun hay cash entonces pago la factura completa sino la pago incompleta
                                    payment.Invoices.SumApplied = invoice.TotalAmount <= amountLeft ? invoice.TotalAmount : amountLeft;
                                    amountLeft -= payment.Invoices.SumApplied;
                                    payment.Invoices.Add();
                                }
                            }
                        }

                        int errorCode = payment.Add();

                        if (errorCode != 0)
                        {
                            lastMessage = "Error Code: "
                                        + company.GetLastErrorCode().ToString()
                                        + " - "
                                        + company.GetLastErrorDescription();
                        }
                        else
                        {
                            String key = company.GetNewObjectKey();
                            p.DocEntry = key;
                            db.Entry(p).State = System.Data.Entity.EntityState.Modified;
                            db.SaveChanges();
                        }

                        company.Disconnect();
                    }
                    else
                    {
                        lastMessage = "Error Msg: "
                                    + _connection.GetErrorMessage().ToString();
                    }

                    p.LastErrorMessage = lastMessage;
                    db.Entry(p).State = System.Data.Entity.EntityState.Modified;
                    db.SaveChanges();
                }

                return lastMessage;
            }
        }
    }
}