using System;
using System.Xml;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;

class Program
{
	static void Main(string[] args)
	{

		// Create an XmlDocument object.
		XmlDocument xmlDoc = new XmlDocument();

		// Load an XML file into the XmlDocument object.
		try
		{
			xmlDoc.PreserveWhitespace = true;
			xmlDoc.Load("test.xml");
		}
		catch (Exception e)
		{
			Console.WriteLine(e.Message);
		}

		// Create a new RSA key.  This key will encrypt a symmetric key,
		// which will then be imbedded in the XML document.  
		RSA rsaKey = new RSACryptoServiceProvider();


		try
		{
			// Encrypt the "creditcard" element.
			Encrypt(xmlDoc, "creditcard", rsaKey, "rsaKey");

			// Inspect the EncryptedKey element.
			InspectElement(xmlDoc);

			// Decrypt the "creditcard" element.
			Decrypt(xmlDoc, rsaKey, "rsaKey");

		}
		catch (Exception e)
		{
			Console.WriteLine(e.Message);
		}
		finally
		{
			// Clear the RSA key.
			rsaKey.Clear();
		}

	}

	public static void Encrypt(XmlDocument Doc, string ElementToEncrypt, RSA Alg, string KeyName)
	{
		// Check the arguments.  
		if (Doc == null)
			throw new ArgumentNullException("Doc");
		if (ElementToEncrypt == null)
			throw new ArgumentNullException("ElementToEncrypt");
		if (Alg == null)
			throw new ArgumentNullException("Alg");

		////////////////////////////////////////////////
		// Find the specified element in the XmlDocument
		// object and create a new XmlElemnt object.
		////////////////////////////////////////////////

		XmlElement elementToEncrypt = Doc.GetElementsByTagName(ElementToEncrypt)[0] as XmlElement;

		// Throw an XmlException if the element was not found.
		if (elementToEncrypt == null)
		{
			throw new XmlException("The specified element was not found");

		}

		//////////////////////////////////////////////////
		// Create a new instance of the EncryptedXml class 
		// and use it to encrypt the XmlElement with the 
		// a new random symmetric key.
		//////////////////////////////////////////////////

		// Create a 256 bit Rijndael key.
		RijndaelManaged sessionKey = new RijndaelManaged();
		sessionKey.KeySize = 256;

		EncryptedXml eXml = new EncryptedXml();

		byte[] encryptedElement = eXml.EncryptData(elementToEncrypt, sessionKey, false);

		////////////////////////////////////////////////
		// Construct an EncryptedData object and populate
		// it with the desired encryption information.
		////////////////////////////////////////////////


		EncryptedData edElement = new EncryptedData();
		edElement.Type = EncryptedXml.XmlEncElementUrl;

		// Create an EncryptionMethod element so that the 
		// receiver knows which algorithm to use for decryption.

		edElement.EncryptionMethod = new EncryptionMethod(EncryptedXml.XmlEncAES256Url);

		// Encrypt the session key and add it to an EncryptedKey element.
		EncryptedKey ek = new EncryptedKey();

		byte[] encryptedKey = EncryptedXml.EncryptKey(sessionKey.Key, Alg, false);

		ek.CipherData = new CipherData(encryptedKey);

		ek.EncryptionMethod = new EncryptionMethod(EncryptedXml.XmlEncRSA15Url);

		// Set the KeyInfo element to specify the
		// name of the RSA key.

		// Create a new KeyInfo element.
		edElement.KeyInfo = new KeyInfo();

		// Create a new KeyInfoName element.
		KeyInfoName kin = new KeyInfoName();

		// Specify a name for the key.
		kin.Value = KeyName;

		// Add the KeyInfoName element to the 
		// EncryptedKey object.
		ek.KeyInfo.AddClause(kin);

		// Add the encrypted key to the 
		// EncryptedData object.

		edElement.KeyInfo.AddClause(new KeyInfoEncryptedKey(ek));

		// Add the encrypted element data to the 
		// EncryptedData object.
		edElement.CipherData.CipherValue = encryptedElement;

		////////////////////////////////////////////////////
		// Replace the element from the original XmlDocument
		// object with the EncryptedData element.
		////////////////////////////////////////////////////

		EncryptedXml.ReplaceElement(elementToEncrypt, edElement, false);

	}

	public static void Decrypt(XmlDocument Doc, RSA Alg, string KeyName)
	{
		// Check the arguments.  
		if (Doc == null)
			throw new ArgumentNullException("Doc");
		if (Alg == null)
			throw new ArgumentNullException("Alg");
		if (KeyName == null)
			throw new ArgumentNullException("KeyName");

		// Create a new EncryptedXml object.
		EncryptedXml exml = new EncryptedXml(Doc);

		// Add a key-name mapping.
		// This method can only decrypt documents
		// that present the specified key name.
		exml.AddKeyNameMapping(KeyName, Alg);

		// Decrypt the element.
		exml.DecryptDocument();

	}

	static void InspectElement(XmlDocument Doc)
	{
		// Get the EncryptedKey element from the XMLDocument object.
		XmlElement encryptedKey = Doc.GetElementsByTagName("EncryptedKey")[0] as XmlElement;

		// Create a new EncryptedKey object.
		EncryptedKey k = new EncryptedKey();

		// Load the XML from the document to
		// initialize the EncryptedKey object.
		k.LoadXml(encryptedKey);

		// Display the properties.
		// Most values are Null by default.
		Console.WriteLine("EncryptedKey.KeyInfo: " + k.KeyInfo.GetXml().InnerXml);
		Console.WriteLine("EncryptedKey.Id: " + k.Id);
		Console.WriteLine("EncryptedKey.CarriedKeyName: " + k.CarriedKeyName);
		Console.WriteLine("EncryptedKey.CipherData: " + k.CipherData.GetXml().InnerXml);
		Console.WriteLine("EncryptedKey.Encoding: " + k.Encoding);
		Console.WriteLine("EncryptedKey.EncryptionMethod: " + k.EncryptionMethod.GetXml().InnerXml);
		if (k.EncryptionProperties.Count >= 1)
		{
			Console.WriteLine("EncryptedKey.EncryptionProperties: " + k.EncryptionProperties[0].GetXml().InnerXml);
		}
		Console.WriteLine("EncryptedKey.MimeType: " + k.MimeType);
		Console.WriteLine("EncryptedKey.Recipient: " + k.Recipient);

		if (k.ReferenceList.Count >= 1)
		{
			Console.WriteLine("EncryptedKey.ReferenceList: " + k.ReferenceList[0].GetXml().InnerXml);
		}
	}

}