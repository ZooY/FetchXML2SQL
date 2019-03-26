using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Newtonsoft.Json;
using Formatting = Newtonsoft.Json.Formatting;


namespace FetchXML_to_SQL_Query
{
    public class Fetch
    {
        public int Top { get; set; }
        public bool NoLock { get; set; }
        public Entity Entity { get; set; }
    }


    public class Entity
    {
        public string Name { get; set; }
        public List<string> Attributes { get; set; }
        public Filter Filter { get; set; }
        public List<LinkedEntity> LinkedEntities { get; set; }
        public List<Order> Orders { get; set; }
    }


    public class LinkedEntity : Entity
    {
        public string LinkedEntityAttribute { get; set; }
        public string ParentEntityAttribute { get; set; }
    }


    public class Filter
    {
        public List<Condition> Conditions { get; set; }
     }


    public class Condition
    {
        public string Attribute { get; set; }
        public string Operator { get; set; }
        public string Value { get; set; }
    }


    public class Order
    {
        public string Attribute { get; set; }
        public bool Descending { get; set; }
    }


    class Program
    {
        static void Main(string[] args)
        {
            var fetchXml = @"
<fetch top='50' no-lock='true' >
  <entity name='account' >
    <attribute name='name' />
    <attribute name='address' />
    <filter>
      <condition attribute='createdon' operator='lt' value='2019-03-21' />
    </filter>
    <link-entity name='contact' from='parentcustomerid' to='accountid' >
      <attribute name='fullname' />
      <order attribute='firstname' />
    </link-entity>
  </entity>
</fetch>";


            var br = Environment.NewLine;
            var xml = new XmlDocument();
            xml.LoadXml(fetchXml);
            var fetchNode = xml.FirstChild;
            var entityNode = fetchNode.FirstChild;

            var fetch = new Fetch
            {
                Top = GetTop(fetchNode),
                NoLock = GetNoLock(fetchNode),
                Entity = GetLinkedEntity(entityNode)
            };

            // SELECT options
            var selectOptions = "";
            if (fetch.Top > 0)
                selectOptions = $"TOP {fetch.Top}";

            // SELECT columns
            var selectColumns = fetch.Entity.Attributes;

            // FROM
            var from = fetch.Entity.Name;

            // WHERE
            string where = null;
            var whereList = new List<string>();
            foreach (var condition in fetch.Entity.Filter.Conditions)
            {
                whereList.Add(condition.Attribute + " = '" + condition.Value + "'");
            }

            where = string.Join($",{br}    ", whereList);


            var query = "SELECT " + selectOptions;
            query += br + "    " + string.Join($",{br}    ", selectColumns);
            query += br + "FROM";
            query += br + "    " + from;
            if (where != null)
            {
                query += br + "WHERE";
                query += br + "    " + where;
            }

            Console.WriteLine(query);
            Console.WriteLine();
            //Console.WriteLine(JsonConvert.SerializeObject(fetch, Formatting.Indented, new JsonSerializerSettings
            //{
            //    NullValueHandling = NullValueHandling.Ignore
            //}));
            Console.ReadKey();
        }


        private static int GetTop(XmlNode fetchNode)
        {
            var tmpStr = fetchNode.Attributes["top"]?.Value;
            return !string.IsNullOrWhiteSpace(tmpStr) && int.TryParse(tmpStr, out var tmpInt) ? tmpInt : 0;
        }


        private static bool GetNoLock(XmlNode fetchNode)
        {
            var tmpStr = fetchNode.Attributes["no-lock"]?.Value;
            return !string.IsNullOrWhiteSpace(tmpStr) && bool.TryParse(tmpStr, out var tmpBool) ? tmpBool : false;
        }


        private static LinkedEntity GetLinkedEntity(XmlNode entityNode)
        {
            return new LinkedEntity
            {
                LinkedEntityAttribute = entityNode.Attributes["from"]?.Value,
                ParentEntityAttribute = entityNode.Attributes["to"]?.Value,
                Name = GetEntityName(entityNode),
                Attributes = GetEntityAttributes(entityNode),
                Filter = GetEntityFilter(entityNode),
                LinkedEntities = GetLinkedEntities(entityNode),
                Orders = GetOrders(entityNode)
            };
        }


        private static string GetEntityName(XmlNode entityNode)
        {
           return entityNode.Attributes["name"].Value;
        }


        private static List<string> GetEntityAttributes(XmlNode entityNode)
        {
            var attributes = new List<string>();
            foreach (XmlNode node in entityNode.ChildNodes)
            {
                if (node.Name.Equals("attribute", StringComparison.OrdinalIgnoreCase))
                    attributes.Add(node.Attributes["name"].Value);
            }

            return attributes;
        }


        private static Filter GetEntityFilter(XmlNode entityNode)
        {
            var filterNode = entityNode.ChildNodes.GetNodeByName("filter");
            if (filterNode == null)
                return null;
            var conditions = GetConditions(filterNode);
            if (conditions.Any())
                return new Filter { Conditions = conditions };
            return null;
        }


        private static List<Condition> GetConditions(XmlNode filterNode)
        {
            var conditions = new List<Condition>();
            foreach (XmlNode conditionNode in filterNode.ChildNodes)
            {
                conditions.Add(new Condition
                {
                    Attribute = GetConditionAttribute(conditionNode),
                    Operator = GetConditionOperator(conditionNode),
                    Value = GetConditionValue(conditionNode)
                });
            }
            return conditions;
        }


        private static string GetConditionAttribute(XmlNode conditionNode)
        {
            return conditionNode.Attributes["attribute"]?.Value;
        }


        private static string GetConditionOperator(XmlNode conditionNode)
        {
            return conditionNode.Attributes["operator"]?.Value;
        }


        private static string GetConditionValue(XmlNode conditionNode)
        {
            return conditionNode.Attributes["value"]?.Value;
        }


        private static List<LinkedEntity> GetLinkedEntities(XmlNode entityNode)
        {
            var linkedEntities = new List<LinkedEntity>();
            foreach (XmlNode node in entityNode.ChildNodes)
            {
                if (node.Name.Equals("link-entity", StringComparison.OrdinalIgnoreCase))
                    linkedEntities.Add(GetLinkedEntity(node));
            }
            return linkedEntities.Count > 0 ? linkedEntities : null;
        }


        private static List<Order> GetOrders(XmlNode entityNode)
        {
            var orders = new List<Order>();
            foreach (XmlNode node in entityNode.ChildNodes)
            {
                if (node.Name.Equals("order", StringComparison.OrdinalIgnoreCase))
                {
                    orders.Add(new Order
                    {
                        Attribute = node.Attributes["attribute"]?.Value,
                        Descending = bool.TryParse(node.Attributes["descending"]?.Value, out bool descending) ? descending : false
                    });
                }
            }
            return orders.Count > 0 ? orders : null;
        }
    }


    public static class XmlNodeListExtensions
    {
        public static XmlNode GetNodeByName(this XmlNodeList nodes, string name)
        {
            foreach (XmlNode node in nodes)
                if (node.Name.Equals("filter", StringComparison.OrdinalIgnoreCase))
                    return node;
            return null;
        }
    }
}
