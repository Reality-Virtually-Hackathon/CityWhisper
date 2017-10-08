using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.IO;
using UnityEngine;

public class XMLSerializer {
	public static string Serialize(Message1 obj) {
		XmlSerializer serializer = new XmlSerializer(obj.GetType());

		using (StringWriter writer = new StringWriter ()) {
			serializer.Serialize(writer, obj);

			return writer.ToString();
		}
	}

	public static Message1 Deserialize(string serializedStr) {
		XmlSerializer serializer = new XmlSerializer(typeof(Message1));

		using (TextReader reader = new StringReader (serializedStr)) {
			return (Message1) serializer.Deserialize(reader);
		}
	}
}
