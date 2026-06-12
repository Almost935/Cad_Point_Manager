using netDxf.Entities;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public static List<ParsedMLeader> Read(string dxfPath)
        {
            var tags = DxfTagReader.ReadTags(dxfPath);
            var leaders = new List<ParsedMLeader>();

            for (int i = 0; i < tags.Count; i++)
            {
                if (tags[i].Code == 0 &&
                    tags[i].Value == "MULTILEADER")
                {
                    int start = i;

                    i++;

                    while (i < tags.Count &&
                           tags[i].Code != 0)
                    {
                        i++;
                    }

                    leaders.Add(
                        ParseEntity(
                            tags.GetRange(
                                start,
                                i - start)));

                    i--;
                }
            }

            return leaders;
        }

        private static ParsedMLeader ParseEntity(
            List<DxfTag> tags)
        {
            var mleader =
                new ParsedMLeader();

            ParsedLeaderNode? currentLeader =
                null;

            ParsedLeaderLine? currentLine =
                null;

            ParseState state =
                ParseState.Entity;

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
    }
}
