using Microsoft.Configuration.ConfigurationBuilders;
using System;
using System.ServiceModel.Configuration;
using System.Linq;
using System.Collections.Generic;

namespace Microsoft.ConfigurationBuilders.Base
{
    /// <summary>
    /// A class that can be used by <see cref="KeyValueConfigBuilder"/>s to apply key/value config pairs to <see cref="ClientSection"/> Endpoints.
    /// </summary>
    public class ServiceModelClientEndpointsSectionHandler : SectionHandler<ClientSection>
    {
        /// <summary>
        /// Updates an existing endpoint address in the assigned <see cref="SectionHandler{T}.ConfigSection"/> with a new name and a new address. The old
        /// endpoint can be located using the <paramref name="oldItem"/> parameter. If an old endpoint is not found, a new endpoint
        /// should be inserted.
        /// </summary>
        /// <param name="newKey">The updated key name for the endpoint.</param>
        /// <param name="newValue">The updated value for the endpoint.</param>
        /// <param name="oldKey">The old key name for the endpoint, or null.</param>
        /// <param name="oldItem">A reference to the old <see cref="ChannelEndpointElement"/> object obtained by <see cref="GetEnumerator"/>, or null.</param>
        public override void InsertOrUpdate(string newKey, string newValue, string oldKey = null, object oldItem = null)
        {
            if (newValue != null)
            {
                ChannelEndpointElement[] endpoints = new ChannelEndpointElement[ConfigSection.Endpoints.Count];
                ConfigSection.Endpoints.CopyTo(endpoints, 0);

                var oldKeyEndpoint = endpoints.FirstOrDefault(x => x.Name == oldKey);
                // Preserve the old entry if it exists, as it might have more than just name/address attributes.
                var cee = (oldItem as ChannelEndpointElement) ?? oldKeyEndpoint;

                var newKeyEndpoint = endpoints.FirstOrDefault(x => x.Name == newKey);

                // Make sure there are no entries using the old or new name other than this one
                if (oldKeyEndpoint != null)
                    ConfigSection.Endpoints.Remove(oldKeyEndpoint);

                if (oldKey != newKey && newKeyEndpoint != null)
                    ConfigSection.Endpoints.Remove(newKeyEndpoint);

                // Update values and re-add to the collection
                cee.Name = newKey;
                cee.Address = new Uri(newValue);
                ConfigSection.Endpoints.Add(cee);
            }
        }

        /// <summary>
        /// Gets an <see cref="IEnumerator{T}"/> that iterates over the key/value pairs contained in the assigned <see cref="SectionHandler{T}.ConfigSection"/>. />
        /// </summary>
        /// <returns>An enumerator over pairs where the key is the existing name for each setting in the system.serviceModel/client section, and the value
        /// is a reference to the <see cref="ChannelEndpointElement"/> object referred to by that name.</returns>
        public override IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            // The Endpoints collection may change on us while we enumerate. :/
            ChannelEndpointElement[] endpoints = new ChannelEndpointElement[ConfigSection.Endpoints.Count];
            ConfigSection.Endpoints.CopyTo(endpoints, 0);

            foreach (ChannelEndpointElement cee in endpoints)
                yield return new KeyValuePair<string, object>(cee.Name, cee);
        }

        /// <summary>
        /// Attempt to lookup the original key casing so it can be preserved during greedy updates which would otherwise lose
        /// the original casing in favor of the casing used in the config source.
        /// </summary>
        /// <param name="requestedKey">The key to find original casing for.</param>
        /// <returns>A string containing the key with original casing from the config section, or the key as passed in if no match
        /// can be found.</returns>
        public override string TryGetOriginalCase(string requestedKey)
        {
            if (!String.IsNullOrWhiteSpace(requestedKey))
            {
                ChannelEndpointElement[] endpoints = new ChannelEndpointElement[ConfigSection.Endpoints.Count];
                ConfigSection.Endpoints.CopyTo(endpoints, 0);

                var endpoint = Array.Find(endpoints, x => x.Name == requestedKey);
                if (endpoint != null)
                    return endpoint.Name;
            }

            return base.TryGetOriginalCase(requestedKey);
        }
    }
}
