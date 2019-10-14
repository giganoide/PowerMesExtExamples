using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Atys.PowerMES.Foundation;

namespace TeamSystem.Customizations
{
    /// <summary>
    /// Metodi statici o extensio per la gestione degli attributi
    /// </summary>
    internal static class AttributesHelpers
    {
        /// <summary>
        /// Restituisce se la risorsa ha un attributo attivo che abilita
        /// la procedura di intestatura
        /// </summary>
        /// <param name="resource">Risorsa di cui valutare gli attributi</param>
        /// <param name="attributeName">Attributo da ricercare</param>
        /// <returns>Se l'attributo è presente, attivo e con valore 1</returns>
        public static bool HasActiveBooleanAttribute(this IMesResource resource, string attributeName)
        {
            if (resource == null
               || resource.Settings.ResourceAttributes == null)
                return false;

            var outResult = false;

            var isValid = resource.Settings.ResourceAttributes.TryGetActiveAttribute(attributeName, ValueContainerType.Boolean, out outResult);

            return isValid && outResult;
        }

        /// <summary>
        /// Verifica se è presente un attributo di tipo stringa e ne restituisce il valore
        /// </summary>
        /// <param name="resource">Risorsa di cui valutare gli attributi</param>
        /// <param name="attributeName">Attributo da ricercare</param>
        /// <param name="attributeValue">Valore estratto dall'attributo</param>
        /// <returns>Se l'attributo è presente e con un valore valido</returns>
        public static bool TryGetActiveResourceStringAttribute(this IMesResource resource, string attributeName,
                                                               out string attributeValue)
        {
            attributeValue = string.Empty;

            if (resource == null
               || resource.Settings.ResourceAttributes == null)
                return false;

            var isValid = resource.Settings
                                  .ResourceAttributes
                                  .TryGetActiveAttribute(attributeName, ValueContainerType.String, out attributeValue);

            return isValid;
        }

        /// <summary>
        /// Verifica se è presente un attributo di tipo stringa e ne restituisce il valore
        /// </summary>
        /// <param name="attributes">Lista attributi</param>
        /// <param name="attributeName">Attributo da ricercare</param>
        /// <param name="containerType">Tipo di dato</param>
        /// <param name="attributeValue">Valore estratto dall'attributo</param>
        /// <returns>Se l'attributo è presente e con un valore valido</returns>
        public static bool TryGetActiveAttribute<T>(this List<GenericValueContainer> attributes, string attributeName,
                                                    ValueContainerType containerType,
                                                    out T attributeValue)
        {
            attributeValue = default(T);

            if (attributes == null
               || string.IsNullOrWhiteSpace(attributeName))
                return false;

            var attribute = attributes.FirstOrDefault(a => a.Name == attributeName);
            if (attribute == null)
                return false;

            var isValid = attribute.IsValid
                          && !attribute.Disabled
                          && attribute.Type == containerType
                          && attribute.TryConvertValueToType(out attributeValue);

            return isValid;
        }
        /// <summary>
        /// Restituisce il valore di un attributo, oppure il default
        /// del tipo richiesto se non presente o in caso di qualsiasi errore
        /// </summary>
        /// <typeparam name="T">Tipo del dato richiesto</typeparam>
        /// <param name="attributes">Lista attributi</param>
        /// <param name="attributeName">Attributo da ricercare</param>
        /// <param name="containerType">Tipo del contenitore dati da cercare</param>
        /// <returns>Valore dell'attributo o default del suo tipo</returns>
        public static T GetActiveAttributeValueOrDefault<T>(this List<GenericValueContainer> attributes, string attributeName,
                                                            ValueContainerType containerType)
        {
            T attributeValue;

            var isValid = attributes.TryGetActiveAttribute(attributeName, containerType, out attributeValue);

            return isValid
                       ? attributeValue
                       : default(T);
        }
    }
}
