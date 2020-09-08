///////////////////////////////////////////////////////////
//  KorrespondansepartIntern.cs
//  Implementation of the Class KorrespondansepartIntern
//  Generated by Enterprise Architect
//  Created on:      02-jun-2020 09:26:04
//  Original author: Tor Kjetil
///////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;




namespace FIKS.eMeldingArkiv.eMeldingForenkletArkiv {
	public class KorrespondansepartIntern {

		public string administrativEnhet;
		/// <summary>
		/// referanse til AdministrativEnhet sin systemID
		/// </summary>
		public string referanseAdministrativEnhet;
		public string saksbehandler;
		/// <summary>
		/// referanse til Bruker sin systemID
		/// </summary>
		public string referanseSaksbehandler;

		public KorrespondansepartIntern(){

		}

		~KorrespondansepartIntern(){

		}

	}//end KorrespondansepartIntern

}//end namespace eMeldingForenkletArkiv