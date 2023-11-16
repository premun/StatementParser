using System;
namespace StatementParser.Models
{
	public abstract class Transaction
	{
		public Broker Broker { get; }
		public DateTime Date { get; }
		public string Name { get; }
		public Currency Currency { get; }

		protected Transaction(Broker broker, DateTime date, string name, Currency currency)
		{
			this.Broker = broker;
			this.Date = date;
			this.Name = name ?? throw new ArgumentNullException(nameof(name));
			this.Currency = currency;
		}

		public abstract decimal Amount { get; }

		public override string ToString()
		{
			return $"Broker: {Broker} Date: {Date.ToShortDateString()} Name: {Name} Currency: {Currency}";
		}
	}
}
