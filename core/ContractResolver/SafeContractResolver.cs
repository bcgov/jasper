using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Reflection;

namespace Scv.Core.Helpers.ContractResolver
{
    /// <summary>
    /// This is used so the Requried field doesn't always apply and look for a null value on properties of the generated classes.
    /// Also helps with AdditionalProperties. 
    /// </summary>
    public class SafeContractResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var jsonProp = base.CreateProperty(member, memberSerialization);
            jsonProp.Required = Required.Default;
            return jsonProp;
        }
    }
}