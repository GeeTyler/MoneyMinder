﻿using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using MoneyMinder.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace MoneyMinder.Data
{
    public class DataAccessService : IDataAccessService
    {
        private readonly DatabaseContext _db;

        public DataAccessService(DatabaseContext db)
        {
            _db = db;
        }

        public List<Stock> GetStocks() 
        {
            return _db.Stock.ToList();
        }

        public List<Stock> GetFilteredStocks(string SearchText)
        {
            return _db.Stock.Where(stock => stock.CompanyName.ToLower().StartsWith(SearchText.ToLower())).ToList();
        }

        public List<Stock> getSortedStock(string filter)
        {
            List<Stock> toSort = GetStocks();
            List<Stock> filtered = new List<Stock>();
            
            if (filter.Contains("CompanyName"))
            {
                filtered.Add(toSort[0]);
                for (int i = 1; i < toSort.Count(); i++)
                {
                    
                    for(int b = 0; b < filtered.Count(); b++)
                    {
                        int compare = toSort[i].CompanyName.ToLower().CompareTo(filtered[b].CompanyName.ToLower());
                        if (b == filtered.Count() - 1)
                        {
                            filtered.Add(toSort[i]);
                            break;
                        }
                        else if (compare < 0)
                        {
                            filtered.Insert(b, toSort[i]);
                            break;
                        }
                    }

                    
                }
                
            }else if (filter.Contains("Market Price"))
            {
                filtered.Add(toSort[0]);
                for (int i = 1; i < toSort.Count(); i++)
                {

                    for (int b = 0; b < filtered.Count(); b++)
                    {
                        int compare = toSort[i].MarketPrice.CompareTo(filtered[b].MarketPrice);
                        if (b == filtered.Count() - 1)
                        {
                            filtered.Add(toSort[i]);
                            break;
                        }
                        else if (compare < 0)
                        {
                            filtered.Insert(b, toSort[i]);
                            break;
                        }
                    }
                }
                
            }else if(filter.Contains("Market Capitalisation"))
            {
                filtered.Add(toSort[0]);
                for (int i = 1; i < toSort.Count(); i++)
                {

                    for (int b = 0; b < filtered.Count(); b++)
                    {
                        long compare = Int64.Parse(toSort[i].MarketCap.Replace("$", "").Replace(",",""))- Int64.Parse(filtered[b].MarketCap.Replace("$", "").Replace(",", ""));
                        if (b == filtered.Count() - 1)
                        {
                            filtered.Add(toSort[i]);
                            break;
                        }
                        else if (compare < 0)
                        {
                            filtered.Insert(b, toSort[i]);
                            break;
                        }
                    }
                }
            }

            return filtered;
        }

        public List<MarketPriceData> GetMarketPrices() 
        {
            return _db.MarketPriceData.ToList();
        }

        public List<BankAccount> GetBankAccounts(string UserEmail) 
        {
            if (_db.BankAccount == null) 
            {
                return new List<BankAccount>();
            }
            else
            {
                return _db.BankAccount.Where(account => account.Email.Contains(UserEmail)).ToList();
            }
        }

        public BankAccount GetBankAccount(int Account) 
        {
            return _db.BankAccount.FirstOrDefault(selected => selected.AccountNum == Account);
        }

        public void AddBankAccount(string UserEmail, string AccountName)
        {
            var random = new Random();
            bool accountNumberExists = true;
            int randomAccountNum = 0;

            while (accountNumberExists)
            {
                randomAccountNum = new Random().Next(100000000, 999999999);

                var existingAccount = _db.BankAccount.FirstOrDefault(a => a.AccountNum == randomAccountNum);

                if (existingAccount == null)
                {
                    accountNumberExists = false;
                }
            }

            var balance = random.NextDouble() * (10000 - 500) + 500;

            if (!AccountName.Equals(null))
            {
                using (var transaction = _db.Database.BeginTransaction())
                {

                    var account = new BankAccount()
                    {
                        AccountNum = randomAccountNum,
                        Email = UserEmail,
                        Name = AccountName,
                        Balance = balance,
                        Blocked = false,
                    };

                    _db.BankAccount.Add(account);
                    _db.Database.ExecuteSqlRaw("SET IDENTITY_INSERT dbo.BankAccount ON;");
                    _db.SaveChanges();
                    _db.Database.ExecuteSqlRaw("SET IDENTITY_INSERT dbo.BankAccount OFF;");
                    transaction.Commit();
                }
            }
        }

        public void ChangeBankAccountName(int AccountNum, string AccountName) 
        {
            _db.BankAccount.FirstOrDefault(account => account.AccountNum.Equals(AccountNum)).Name = AccountName;
            _db.SaveChanges();
        }

        public void BlockBankAccount(int AccountNum, bool isBlocked) 
        {
            _db.BankAccount.FirstOrDefault(account => account.AccountNum.Equals(AccountNum)).Blocked = isBlocked;
            _db.SaveChanges();
        }

        public User GetUser(string UserEmail)
        {
            return _db.User.FirstOrDefault(user => user.Email.Equals(UserEmail));
        }

        public void ChangeFirstName(string Input, string UserEmail) 
        {
            _db.User.FirstOrDefault(user => user.Email.Equals(UserEmail)).FirstName = Input;
            _db.SaveChanges();
        }

        public void ChangeLastName(string Input, string UserEmail)
        {
            _db.User.FirstOrDefault(user => user.Email.Equals(UserEmail)).LastName = Input;
            _db.SaveChanges();
        }

        public List<Transactions> GetTransactions(int AccountNum) 
        {
            return _db.Transactions.Where(a => a.AccountNum == AccountNum).ToList();
        }

        string ChosenStockCode;

        public void SetChosenStock(string Code)
        {
            ChosenStockCode = Code;
        }

        public string GetChosenStock() 
        {
            return ChosenStockCode;
        }

        public void DeleteUsersInfo(string Email)
        {
            var accounts = _db.BankAccount.Where(a => a.Email == Email).ToList();

            _db.BankAccount.RemoveRange(accounts);

            var users = _db.User.Where(a => a.Email == Email).ToList();

            _db.User.RemoveRange(users);

            _db.SaveChanges();
        }

        public void DeleteBankAccount(int AccountNum)
        {
            var accounts = _db.BankAccount.Where(a => a.AccountNum == AccountNum).ToList();

            _db.BankAccount.RemoveRange(accounts);

            _db.SaveChanges();
        }

        public void AddTransfer(int accountNum, int ToThisAccount, double Amount)
        {
            if(Amount <= 0)
            {
                return;
            }
            var random = new Random();
            bool TranscationNumberExists = true;
            int RandomTransactionNum = 0;
            int RandomTransactionNumTwo = 0;

            while (TranscationNumberExists)
            {
                RandomTransactionNum = new Random().Next(1, 9999999);
                RandomTransactionNumTwo = new Random().Next(1, 9999999);

                var existingAccount = _db.Transactions.FirstOrDefault(a => a.TrasactionNum == RandomTransactionNum 
                || a.TrasactionNum == RandomTransactionNumTwo);

                if (existingAccount == null)
                {
                    TranscationNumberExists = false;
                }
            }

            var fromAccount = _db.BankAccount.FirstOrDefault(a => a.AccountNum == accountNum);

            var toAccount = _db.BankAccount.FirstOrDefault(a => a.AccountNum == ToThisAccount);

            fromAccount.Balance -= Amount;

            toAccount.Balance += Amount;

            var fromTransaction = new Transactions()
            {
                TrasactionNum = RandomTransactionNum,
                TransactionType = "Transfer",
                AccountNum = accountNum,
                TransactionAmount = -Amount,
                DateandTime = DateTime.Now,
            };
            _db.Transactions.Add(fromTransaction);

            var toTransaction = new Transactions()
            {
                TrasactionNum = RandomTransactionNumTwo,
                TransactionType = "Receive Transfer",
                AccountNum = ToThisAccount,
                TransactionAmount = Amount,
                DateandTime = DateTime.Now,
            };
            _db.Transactions.Add(toTransaction);

            _db.SaveChanges();

        }

        public void GenerateRandomTransactions(int AccountNum)
        {
            var random = new Random();
            bool TranscationNumberExists = true;
            int RandomTransactionNum = 0;

            while (TranscationNumberExists)
            {
                RandomTransactionNum = new Random().Next(1, 9999999);

                var existingAccount = _db.Transactions.FirstOrDefault(a => a.TrasactionNum == RandomTransactionNum);

                if (existingAccount == null)
                {
                    TranscationNumberExists = false;
                }
            }

            string[] types = { "Bills", "Eating Out", "Entertainment","Shopping", "Supermarket" };

            int RandomType = random.Next(0, 5);
            
            DateTime RandomDay()
            {
                DateTime start = new DateTime(2023, 1, 1);
                int range = (DateTime.Today - start).Days;
                return start.AddDays(random.Next(range));
            }

            double randomTransactionAmount = random.NextDouble() * (1000 - 1) + 1;

            using (var transaction = _db.Database.BeginTransaction())
            {

                var transactionMade = new Transactions()
                {
                    TrasactionNum = RandomTransactionNum,
                    AccountNum = AccountNum,
                    DateandTime = RandomDay(),
                    TransactionAmount = randomTransactionAmount * -1,
                    TransactionType = types[RandomType],
                };

                _db.Transactions.Add(transactionMade);
                _db.Database.ExecuteSqlRaw("SET IDENTITY_INSERT dbo.Transactions ON;");
                _db.SaveChanges();
                _db.Database.ExecuteSqlRaw("SET IDENTITY_INSERT dbo.Transactions OFF;");
                transaction.Commit();
            }
        }
    }
}
