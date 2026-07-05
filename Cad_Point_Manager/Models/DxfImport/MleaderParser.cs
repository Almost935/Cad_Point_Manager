namespace Cad_Point_Manager.Models.DxfImport
{
    public static class MleaderParser
    {
        private enum ParseState
        {
            Entity,
            ContextData,
            Leader,
            LeaderLine
        }

        public static MLeaderParseResult Read(string dxfPath)
        {
            var tags = DxfTagReader.ReadTags(dxfPath);

            var result = new MLeaderParseResult();

            for (int i = 0; i < tags.Count; i++)
            {
                if (tags[i].Code != 0) { continue; }

                int start = i;

                i++;

                while (i < tags.Count && tags[i].Code != 0)
                {
                    i++;
                }

                var objectTags = tags.GetRange(start, i - start);

                //----------------------------------
                // Build handle lookup
                //----------------------------------

                string? handle =
                    objectTags
                        .FirstOrDefault(
                            x => x.Code == 5)
                        .Value;

                if (!string.IsNullOrWhiteSpace(handle))
                {
                    result.ObjectsByHandle[handle] = objectTags;
                }

                //----------------------------------
                // Parse object type
                //----------------------------------

                string objectType = objectTags[0].Value;

                switch (objectType)
                {
                    case "MULTILEADER":
                        {
                            result.MLeaders.Add(ParseEntity(objectTags));

                            break;
                        }

                    case "MLEADERSTYLE":
                        {
                            var style =
                                ParseStyle(
                                    objectTags);

                            result.MLeaderStyles[
                                style.Handle] = style;

                            break;
                        }

                    case "BLOCK_RECORD":
                        {
                            var block =
                                ParseBlockRecord(
                                    objectTags);

                            result.BlockRecords[
                                block.Handle] = block;

                            break;
                        }
                    
                    case "DICTIONARY":
                        {
                            var dictionary =
                                ParseDictionary(objectTags);

                            result.Dictionaries[
                                dictionary.Handle] = dictionary;

                            break;
                        }
                }

                i--;
            }

            //----------------------------------
            // Resolve styles
            //----------------------------------

            foreach (var mleader in result.MLeaders)
            {
                if (result.MLeaderStyles.TryGetValue(
                        mleader.LeaderStyleId,
                        out var style))
                {
                    mleader.Style = style;
                }
            }

            //----------------------------------
            // Resolve MleaderStyle Names
            //----------------------------------

            ParsedDictionary mLeaderStyleDictionary = null;

            foreach (var dictionary in result.Dictionaries.Values)
            {
                if (dictionary.Entries.TryGetValue(
                        "ACAD_MLEADERSTYLE",
                        out var styleDictionaryHandle))
                {
                    result.Dictionaries.TryGetValue(
                        styleDictionaryHandle,
                        out mLeaderStyleDictionary);

                    break;
                }
            }
            if (mLeaderStyleDictionary != null)
            {
                foreach (var entry in mLeaderStyleDictionary.Entries)
                {
                    string styleName = entry.Key;

                    string styleHandle = entry.Value;

                    if (result.MLeaderStyles.TryGetValue(
                            styleHandle,
                            out var style))
                    {
                        style.DictionaryName = styleName;
                    }
                }
            }

            //----------------------------------
            // Resolve Arrowhead Types
            //----------------------------------

            foreach (var style in result.MLeaderStyles.Values)
            {
                if (result.BlockRecords.TryGetValue(style.ArrowheadHandle, out var block))
                {
                    if (ArrowheadToNetDxfBlockNameResolver.ResolveBlockName(block.Name, out var arrowheadType))
                    {
                        style.ArrowheadType = arrowheadType;
                    }
                }
            }

            return result;
        }

        private static ParsedBlockRecord ParseBlockRecord(List<DxfTag> tags)
        {
            var block = new ParsedBlockRecord();

            foreach (var tag in tags)
            {
                block.Tags.Add(new MLeaderTag
                {
                    Code = tag.Code,
                    Value = tag.Value
                });
            }

            return block;
        }

        private static ParsedMLeaderStyle ParseStyle(List<DxfTag> tags)
        {
            var style = new ParsedMLeaderStyle();

            foreach (var tag in tags)
            {
                style.Tags.Add(new MLeaderTag
                {
                    Code = tag.Code,
                    Value = tag.Value
                });
            }

            return style;
        }

        private static ParsedMLeader ParseEntity(
            List<DxfTag> tags)
        {
            var mleader = new ParsedMLeader();

            ParsedLeaderNode? currentLeader = null;

            ParsedLeaderLine? currentLine = null;

            ParseState state = ParseState.Entity;

            float? x = null;
            float? y = null;

            foreach (var tag in tags)
            {
                //----------------------------------
                // Context Data
                //----------------------------------
                if (tag.Code == 300 &&
                    tag.Value == "CONTEXT_DATA{")
                {
                    state = ParseState.ContextData;
                    continue;
                }

                if (tag.Code == 301 &&
                    tag.Value == "}")
                {
                    state = ParseState.Entity;
                    continue;
                }

                //----------------------------------
                // Leader Node
                //----------------------------------

                if (tag.Code == 302 &&
                    tag.Value == "LEADER{")
                {
                    currentLeader =
                        new ParsedLeaderNode();

                    mleader.Context
                        .Leaders
                        .Add(currentLeader);

                    state = ParseState.Leader;
                    continue;
                }

                if (tag.Code == 303 &&
                    tag.Value == "}")
                {
                    currentLeader = null;

                    state =
                        ParseState.ContextData;

                    continue;
                }

                //----------------------------------
                // Leader Line
                //----------------------------------

                if (tag.Code == 304 &&
                    tag.Value == "LEADER_LINE{")
                {
                    if (currentLeader == null)
                        continue;

                    currentLine =
                        new ParsedLeaderLine();

                    currentLeader
                        .LeaderLines
                        .Add(currentLine);

                    state =
                        ParseState.LeaderLine;

                    continue;
                }

                if (tag.Code == 305 &&
                    tag.Value == "}")
                {
                    currentLine = null;

                    state =
                        ParseState.Leader;

                    continue;
                }

                //----------------------------------
                // Store Tag
                //----------------------------------

                var mTag =
                    new MLeaderTag
                    {
                        Code = tag.Code,
                        Value = tag.Value
                    };

                switch (state)
                {
                    case ParseState.Entity:
                        mleader.Tags
                            .Add(mTag);
                        break;

                    case ParseState.ContextData:
                        mleader.Context.Tags
                            .Add(mTag);
                        break;

                    case ParseState.Leader:
                        currentLeader?
                            .Tags
                            .Add(mTag);
                        break;

                    case ParseState.LeaderLine:
                        currentLine?
                            .Tags
                            .Add(mTag);
                        break;
                }
            }

            return mleader;
        }

        private static ParsedDictionary ParseDictionary(List<DxfTag> tags)
        {
            var dictionary = new ParsedDictionary();

            foreach (var tag in tags)
            {
                dictionary.Tags.Add(
                    new MLeaderTag
                    {
                        Code = tag.Code,
                        Value = tag.Value
                    });
            }

            string currentName = null;

            foreach (var tag in tags)
            {
                if (tag.Code == 3)
                {
                    currentName = tag.Value;
                }
                else if (
                    (tag.Code == 350 || tag.Code == 360)
                    && currentName != null)
                {
                    dictionary.Entries[currentName] =
                        tag.Value;

                    currentName = null;
                }
            }

            return dictionary;
        }
    }
}
