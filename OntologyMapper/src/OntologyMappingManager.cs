﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.SmartPlaces.Facilities.OntologyMapper
{
    using Microsoft.Azure.DigitalTwins.Parser;
    using Microsoft.Azure.DigitalTwins.Parser.Models;
    using System.Text.RegularExpressions;

    public class OntologyMappingManager : IOntologyMappingManager
    {
        public OntologyMapping OntologyMapping { get; }

        public OntologyMappingManager(IOntologyMappingLoader mappingLoader)
        {
            OntologyMapping = mappingLoader.LoadOntologyMapping();
        }

        public bool TryGetInterfaceRemapDtmi(Dtmi inputDtmi, out DtmiRemap? dtmiRemap)
        {
            dtmiRemap = OntologyMapping.InterfaceRemaps.FirstOrDefault(r => r.InputDtmi == inputDtmi.ToString() && !r.IsIgnored);

            if (dtmiRemap != null)
            {
                return true;
            }

            var namespaceRemap = OntologyMapping.NamespaceRemaps.FirstOrDefault(n => inputDtmi.ToString().Contains(n.InputNamespace));

            if (namespaceRemap != null)
            {
                dtmiRemap = new DtmiRemap()
                {
                    InputDtmi = inputDtmi.ToString(),
                    OutputDtmi = inputDtmi.ToString().Replace(namespaceRemap.InputNamespace, namespaceRemap.OutputNamespace)
                };

                return true;
            }

            return false;
        }

        public bool TryGetRelationshipRemap(string inputRelationship, out RelationshipRemap? relationshipRemap)
        {
            relationshipRemap = OntologyMapping.RelationshipRemaps.FirstOrDefault(r => r.InputRelationship == inputRelationship);

            if (relationshipRemap != null)
            {
                return true;
            }

            return false;
        }

        public bool TryGetPropertyProjection(string outputDtmi, string outputPropertyName, out PropertyProjection? propertyProjection)
        {
            propertyProjection = OntologyMapping.PropertyProjections.OrderBy(e => e.Priority).FirstOrDefault(e => e.OutputPropertyName == outputPropertyName && Regex.Match(outputDtmi, e.OutputDtmiFilter, RegexOptions.IgnoreCase).Success);

            if (propertyProjection != null)
            {
                return true;
            }

            return false;
        }

        public bool TryGetFillProperty(string outputDtmi, string outputPropertyName, out FillProperty? fillProperty)
        {
            fillProperty = OntologyMapping.FillProperties.OrderBy(e => e.Priority).FirstOrDefault(e => e.OutputPropertyName == outputPropertyName && Regex.Match(outputDtmi, e.OutputDtmiFilter, RegexOptions.IgnoreCase).Success);

            if (fillProperty != null)
            {
                return true;
            }

            return false;
        }

        public bool ValidateTargetOntologyMapping(IReadOnlyDictionary<Dtmi, DTEntityInfo> targetObjectModel, out List<string> invalidTargets)
        {
            invalidTargets = new List<string>();

            foreach (var interfaceRemap in OntologyMapping.InterfaceRemaps.Where(ir => !ir.IsIgnored))
            {
                try
                {
                    var outputDtmi = new Dtmi(interfaceRemap.OutputDtmi);
                    if (!targetObjectModel.TryGetValue(outputDtmi, out var dTEntityInfo))
                    {
                        invalidTargets.Add(interfaceRemap.OutputDtmi);
                    }
                }
                catch
                {
                    invalidTargets.Add(interfaceRemap.OutputDtmi);
                }
            }

            return !invalidTargets.Any();
        }
    }
}
