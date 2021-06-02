using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SpamFilterLibrary
{
	public enum MessageType
	{
		Spam,Ham
	}

	public class SpamFilter
	{
		private static int _Max = 1000;
		private static decimal _Significance = 0.5m;

		private int _Trigger = 0;

		private class BaseFact
		{
			public decimal Spam;
			public decimal Ham;

			public bool IsTransparent
			{
				get => ( Math.Abs( ( Spam - Ham ) ) / ( Spam + Ham ) ) <= SpamFilter._Significance;
			}
		}
		static readonly string Exception = @"Data\Exception_words.txt";
		public List<string> _TransparentList = File.ReadAllLines(Exception).ToList();
		
		static readonly string SpamWords = @"Data\2.txt";
		public List<string> _SpamWordsList = File.ReadAllLines(SpamWords).ToList();

		private Dictionary<string, BaseFact> _BaseFacts = new Dictionary<string, BaseFact>( );

		private string[ ] Message2Words ( string Message )
		{
			string szMessage = Message.ToLower( );
			List<char> oPunctuations = szMessage.Where( Char.IsPunctuation ).Distinct( ).ToList( );
			oPunctuations.AddRange( new char[ ] { ' ', '<', '>' } );
			string[ ] oWords = szMessage.Split( oPunctuations.ToArray( ) ).Where( szWord => szWord.Length > 2 ).ToArray( );
			return oWords.Except( _TransparentList ).ToArray( );
		}

		public void Transparent ( )
		{
			_TransparentList.AddRange( _BaseFacts.Where( oItem => oItem.Value.IsTransparent ).ToDictionary( oItem => oItem.Key, oItem => oItem.Value ).Keys.ToList( ) );
			_BaseFacts = _BaseFacts.Where( oItem => !oItem.Value.IsTransparent ).ToDictionary( oItem => oItem.Key, oItem => oItem.Value );
		}

		public void Learn ( string Message, MessageType Type, string[ ] List = null )
		{
			string[ ] szList = List ?? Message2Words( Message );
			decimal nTemp;

			foreach ( string szWord in szList )
			{
				BaseFact oValue = _BaseFacts.ContainsKey( szWord ) ? _BaseFacts[szWord] : new BaseFact( );
				nTemp = (Type == MessageType.Spam) ? oValue.Spam++ : oValue.Ham++;
				_BaseFacts[szWord] = oValue;
			}
		}
		public MessageType Classify ( string Message )
		{
			string[ ] szList = Message2Words( Message );
			Dictionary<string, BaseFact> oList = _BaseFacts.Where( oFact => szList.Contains( oFact.Key ) ).ToDictionary( oItem => oItem.Key, oItem => oItem.Value );
			MessageType eType = oList.Sum( oItem => oItem.Value.Spam ) > oList.Sum( oItem => oItem.Value.Ham ) ? MessageType.Spam : MessageType.Ham;

			Learn( Message, eType, szList );

			if ( ++_Trigger > _Max )
			{
				_Trigger = 0;
				Transparent( );
			}
			return ( eType );
		}
	}
}
