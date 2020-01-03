using UnityEngine;
using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.IO;

namespace Windows
{
	public class ConsoleInput
	{
		//public delegate void InputText( string strInput );
		public event Action<string> OnInputText;
		public string inputString;

		public void ClearLine()
		{
			Console.CursorLeft = 0;
			Console.Write( new String( ' ', Console.BufferWidth ) );
			Console.CursorTop--;
			Console.CursorLeft = 0;
		}

		public void RedrawInputLine()
		{
			if (inputString == null  || inputString.Length == 0 )
                return;

			if ( Console.CursorLeft > 0 )
				ClearLine();

			Console.ForegroundColor = ConsoleColor.Green;
			Console.Write( inputString );
		}

		internal void OnBackspace()
		{
			if ( inputString.Length < 1 ) return;

			inputString = inputString.Substring( 0, inputString.Length - 1 );
			RedrawInputLine();
		}

		internal void OnEscape()
		{
			ClearLine();
			inputString = "";
		}

		internal void OnEnter()
		{
			ClearLine();
			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine( "> " + inputString );

			var strtext = inputString;
			inputString = "";

            OnInputText?.Invoke(strtext);
        }

		public void Update()
		{
			if ( !Console.KeyAvailable ) return;
			var key = Console.ReadKey();
            switch (key.Key)
            {
                case ConsoleKey.Enter:
                    OnEnter();
                    return;
                case ConsoleKey.Backspace:
                    OnBackspace();
                    return;
                case ConsoleKey.Escape:
                    OnEscape();
                    return;
                default:
                    if (key.KeyChar != '\u0000')
                    {
                        inputString += key.KeyChar;
                        RedrawInputLine();
                        return;
                    }
                    return;
            }
		}
	}
}