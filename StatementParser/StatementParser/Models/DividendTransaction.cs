﻿using System;
namespace StatementParser.Models
{
	public class DividendTransaction : Transaction
	{
		/// <summary>
		/// Income before taxation
		/// </summary>
		public decimal Income { get; }

		/// <summary>
		/// Tax represented as positive number
		/// </summary>
		public decimal Tax { get; }

		public override decimal Amount => throw new NotImplementedException();

        public DividendTransaction(DividendTransaction dividendTransaction) : this(dividendTransaction.Broker, dividendTransaction.Date, dividendTransaction.Name, dividendTransaction.Income, dividendTransaction.Tax, dividendTransaction.Currency)
		{
		}

		public DividendTransaction(Broker broker, DateTime date, string name, decimal income, decimal tax, Currency currency) : base(broker, date, name, currency)
		{
			this.Income = income;
			this.Tax = Math.Abs(tax);
		}

		public override string ToString()
		{
			return $"{base.ToString()} Income: {Income} Tax: {Tax}";
		}
	}
}
