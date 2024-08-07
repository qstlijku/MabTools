﻿/* 
 * FCBConverter
 * Copyright (C) 2020  Jakub Mareček (info@jakubmarecek.cz)
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with FCBConverter.  If not, see <https://www.gnu.org/licenses/>.
 */

using Gibbed.Dunia2.FileFormats;
using Gibbed.IO;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.XPath;

namespace FCBConverter.CombinedMoveFile
{
    class OffsetsHashesArray
    {
        public static List<OffsetsHashesArrayItem> offsetsHashesArray = new List<OffsetsHashesArrayItem>();
        public static Dictionary<uint, ulong> offsetsHashesDict = new Dictionary<uint, ulong>();
        //public static Dictionary<uint, ulong> offsetsHashesDict2 = new Dictionary<uint, ulong>();

        public enum ParamNames : uint
        {
            ANIMPARAM = 3324928103,
            POSEANIMPARAM = 698988808,
            MOTIONMATCHINGPARAM = 2395932676,
            BLENDPARAM = 1608936243,
            CURVEPARAM = 3020094394,
            CLIPPARAM = 438514225,
            LAYERPARAM = 1304798653,
            LOOAKATPARAM = 3106312372,
            MOVEBLENDPARAM = 1606123814,
            MOVESTATEPARAM = 3105302744,
            PMSVALUEPARAM = 2803151549,
            RAGDOLLPARAM = 1761566118,
            SECONDARYMOTIONPARAM = 670538874,
            BLENDADJUSTPARAM = 1387024244,
            MOTIONMATCHINGANIMSPEEDPARAM = 2695365952,
            MOTIONMATCHINGANIMMOTIONPARAM = 2309232124
        }

        public static void Deserialize(Dictionary<uint, byte[]> node)
        {
            var offsetsArray = Helpers.UnpackArray(node[CRC32.Hash("offsetsArray")], 4);
            var hashesArray = Helpers.UnpackArray(node[CRC32.Hash("hashesArray")], 8);

            for (int i = 0; i < offsetsArray.Count(); i++)
            {
                offsetsHashesDict.Add(BitConverter.ToUInt32(offsetsArray[i], 0), BitConverter.ToUInt64(hashesArray[i], 0));

                //Program.offsetsHashesArray.Add(new OffsetsHashesArray(BitConverter.ToUInt32(offsetsArray[i], 0), BitConverter.ToUInt64(hashesArray[i], 0), node.NameHash));

                /*writer.WriteStartElement("fixup");
                writer.WriteAttributeString("offset", BitConverter.ToUInt32(offsetsArray[i], 0).ToString());
                writer.WriteAttributeString("hash", Helpers.ByteArrayToString(hashesArray[i]));*/
                /*
                byte[] hashesArrayRev = new byte[hashesArray[i].Length];
                for (int j = 0; j < hashesArray[i].Length; j++)
                {
                    hashesArrayRev[j] = hashesArray[i][j];
                }
                Array.Reverse(hashesArrayRev);
                ulong aaa = ulong.Parse(ByteArrayToString(hashesArrayRev), NumberStyles.HexNumber);
                if (FCBConverter.Program.listFilesDict.ContainsKey(aaa))
                {
                    string name = FCBConverter.Program.listFilesDict[aaa];
                    writer.WriteAttributeString("hash", name);
                }
                else
                    writer.WriteAttributeString("hash", ByteArrayToString(hashesArray[i]));
                */
                //writer.WriteEndElement();

                //Program.combinedMoveFileHelper.Add(node.NameHash.ToString("X8"), offsetsArray[i], hashesArray[i]);
            }
        }

        public static void Serialize(BinaryObject node)
        {
            if (!Enum.IsDefined(typeof(ParamNames), node.NameHash))
                return;

            List<byte[]> fixupsOffsets = new List<byte[]>();
            List<byte[]> fixupsHashes = new List<byte[]>();

            for (int i = 0; i < offsetsHashesArray.Count(); i++)
            {
                if ((uint)offsetsHashesArray[i].param == node.NameHash)
                {
                    fixupsOffsets.Add(BitConverter.GetBytes(offsetsHashesArray[i].offset));
                    fixupsHashes.Add(BitConverter.GetBytes(offsetsHashesArray[i].hash));
                }
            }

            if (fixupsOffsets.Count > 0)
            {
                fixupsOffsets.Insert(0, BitConverter.GetBytes(fixupsOffsets.Count));
                fixupsHashes.Insert(0, BitConverter.GetBytes(fixupsHashes.Count));

                node.Fields.Add(CRC32.Hash("offsetsArray"), fixupsOffsets.SelectMany(byteArr => byteArr).ToArray());
                node.Fields.Add(CRC32.Hash("hashesArray"), fixupsHashes.SelectMany(byteArr => byteArr).ToArray());
            }
        }
    }

    class OffsetsHashesArrayItem
    {
        public uint offset { get; set; }

        public ulong hash { get; set; }

        public OffsetsHashesArray.ParamNames param { get; set; }

        public OffsetsHashesArrayItem(uint offset, ulong hash, uint fixupsParam)
        {
            this.offset = offset;
            this.hash = hash;
            this.param = (OffsetsHashesArray.ParamNames)fixupsParam;
        }

        public OffsetsHashesArrayItem(uint offset, ulong hash, OffsetsHashesArray.ParamNames fixupsParam)
        {
            this.offset = offset;
            this.hash = hash;
            this.param = fixupsParam;
        }
    }

    class PerMoveResourceInfo
    {
        public static List<PerMoveResourceInfoItem> perMoveResourceInfos = new List<PerMoveResourceInfoItem>();

        public static void Deserialize(Dictionary<uint, byte[]> node)
        {
            var sizes = Helpers.UnpackArray(node[CRC32.Hash("sizes")], 4);
            var rootNodeIds = Helpers.UnpackArray(node[CRC32.Hash("rootNodeIds")], 8);
            var resourcePathIds = Helpers.UnpackArray(node[CRC32.Hash("resourcePathIds")], 8);

            for (int i = 0; i < sizes.Count(); i++)
            {
                perMoveResourceInfos.Add(new PerMoveResourceInfoItem(BitConverter.ToUInt32(sizes[i], 0), BitConverter.ToUInt64(rootNodeIds[i], 0), BitConverter.ToUInt64(resourcePathIds[i], 0)));

                /*writer.WriteStartElement("PerMoveResourceInfo");
                writer.WriteAttributeString("size", BitConverter.ToUInt32(sizes[i], 0).ToString());
                writer.WriteAttributeString("rootNodeId", Helpers.ByteArrayToString(rootNodeIds[i]));
                writer.WriteAttributeString("resourcePathId", Helpers.ByteArrayToString(resourcePathIds[i]));*/

                /*
                byte[] resourcePathIdsRev = new byte[resourcePathIds[i].Length];
                for (int j = 0; j < resourcePathIds[i].Length; j++)
                {
                    resourcePathIdsRev[j] = resourcePathIds[i][j];
                }
                Array.Reverse(resourcePathIdsRev);
                ulong aaa = ulong.Parse(ByteArrayToString(resourcePathIdsRev), NumberStyles.HexNumber);
                if (FCBConverter.Program.listFilesDict.ContainsKey(aaa))
                {
                    string name = FCBConverter.Program.listFilesDict[aaa];
                    writer.WriteAttributeString("resourcePathId", name);
                }
                else
                    writer.WriteAttributeString("resourcePathId", ByteArrayToString(resourcePathIds[i]));
                */
                //writer.WriteEndElement();

                //Program.combinedMoveFileHelper.AddSizes(sizes[i], resourcePathIds[i]);
            }
        }

        public static void Serialize(BinaryObject node)
        {
            if (node.NameHash != 1650810464)
                return;

            List<byte[]> sizes = new List<byte[]>();
            List<byte[]> rootNodeIds = new List<byte[]>();
            List<byte[]> resourcePathIds = new List<byte[]>();

            for (int i = 0; i < perMoveResourceInfos.Count(); i++) //perMoveResourceInfos.Count() 10520 2C8A381201B8298B   25315 EE986B52BAA56318
            {
                sizes.Add(BitConverter.GetBytes(perMoveResourceInfos[i].size));
                rootNodeIds.Add(BitConverter.GetBytes(perMoveResourceInfos[i].rootNodeId));
                resourcePathIds.Add(BitConverter.GetBytes(perMoveResourceInfos[i].resourcePathId));
            }

            //string a = Helpers.ByteArrayToString(resourcePathIds[10519]);
            //PerMoveResourceInfoItem perMoveResourceInfoItem = perMoveResourceInfos[25315];

            if (sizes.Count > 0)
            {
                sizes.Insert(0, BitConverter.GetBytes(sizes.Count));
                rootNodeIds.Insert(0, BitConverter.GetBytes(rootNodeIds.Count));
                resourcePathIds.Insert(0, BitConverter.GetBytes(resourcePathIds.Count));

                node.Fields.Add(CRC32.Hash("sizes"), sizes.SelectMany(byteArr => byteArr).ToArray());
                node.Fields.Add(CRC32.Hash("rootNodeIds"), rootNodeIds.SelectMany(byteArr => byteArr).ToArray());
                node.Fields.Add(CRC32.Hash("resourcePathIds"), resourcePathIds.SelectMany(byteArr => byteArr).ToArray());
            }
        }
    }

    class PerMoveResourceInfoItem
    {
        public uint size { get; set; }

        public ulong rootNodeId { get; set; }

        public ulong resourcePathId { get; set; }

        public PerMoveResourceInfoItem(uint size, ulong rootNodeId, ulong resourcePathId)
        {
            this.size = size;
            this.rootNodeId = rootNodeId;
            this.resourcePathId = resourcePathId;
        }
    }

    class MoveBinDataChunk
    {
        XmlWriter writer;
        Stream dataStream;
        uint currentOffset;
        bool isCombined;
        bool isNewDawn;
        bool isFC6;
        bool IncludedLength;

        public MoveBinDataChunk(bool isNewDawn, bool isFC6)
        {
            this.currentOffset = 0;
            this.isCombined = false;
            this.isNewDawn = isNewDawn;
            this.isFC6 = isFC6;
            this.IncludedLength = true;
        }

        public MoveBinDataChunk(uint currentOffset, bool isCombined, bool isNewDawn, bool isFC6, bool IncludedLength)
        {
            this.currentOffset = currentOffset;
            this.isCombined = false; // temporary hack
            //this.isCombined = isCombined;
            this.isNewDawn = isNewDawn;
            this.isFC6 = isFC6;
            this.IncludedLength = IncludedLength;
        }

        private string GetHashFromOffset(bool isShort = false)
        {
            if (isCombined)
            {
                long pos = dataStream.Position;
                Console.WriteLine("Current position: " + pos);
                if (isShort)
                    dataStream.ReadValueU16(); // read
                else
                    dataStream.ReadValueU32(); // read

                string a = OffsetsHashesArray.offsetsHashesDict[(uint)(currentOffset + pos)].ToString("X16");
                Console.WriteLine("Current offset: " + currentOffset);
                Console.WriteLine("Resultant hash: " + a);
                /*
                if (!OffsetsHashesArray.offsetsHashesDict2.ContainsKey((uint)(currentOffset + pos)))
                    OffsetsHashesArray.offsetsHashesDict2.Add((uint)(currentOffset + pos), ulong.Parse(a, NumberStyles.HexNumber));
                    */
                return a;

                throw new Exception("Data mismatch!");
            }
            else
            {
                if (isShort)
                    return dataStream.ReadValueS16().ToString(); // read
                else
                    return dataStream.ReadValueU32().ToString(); // read
            }
        }

        private void GetOffsetFromHash(string value, OffsetsHashesArray.ParamNames type, bool isShort = false)
        {
            if (isCombined)
            {
                long pos = dataStream.Position;
                OffsetsHashesArray.offsetsHashesArray.Add(new OffsetsHashesArrayItem((uint)(currentOffset + pos), ulong.Parse(value, NumberStyles.HexNumber), type));

                if (type == OffsetsHashesArray.ParamNames.PMSVALUEPARAM)
                    dataStream.WriteValueS16(-1);
                else
                {
                    if (isShort)
                        dataStream.WriteValueS16((short)pos);
                    else
                        dataStream.WriteValueU32((uint)pos);
                }
            }
            else
            {
                if (isShort)
                    dataStream.WriteValueS16(short.Parse(value));
                else
                    dataStream.WriteValueU32(uint.Parse(value));
            }
        }

        //******************************************************************************

        public void Deserialize(XmlWriter xmlWriter, byte[] data, ulong rootNodeId = 0)
        {
            writer = xmlWriter;
            dataStream = new MemoryStream(data, IncludedLength ? sizeof(uint) : 0, data.Length - (IncludedLength ? sizeof(uint) : 0));

            ulong filename = dataStream.ReadValueU64(); // moveFileName

            writer.WriteStartElement("CMove_BlendRoot_DTRoot");
            writer.WriteAttributeString("hash", filename.ToString("X16"));

            if (isCombined)
                writer.WriteAttributeString("rootNodeId", rootNodeId.ToString("X16"));

            //writer.WriteAttributeString("debugPurpose", Helpers.ByteArrayToString(data));
            ChildDeserialize();
            writer.WriteEndElement();
        }

        private void ChildDeserialize()
        {
            int endSpace = 0;
            byte childrenCount = 0, unknownA = 0, unknownB = 0, unknown = 0;
            sbyte classNameType = dataStream.ReadValueS8();
            if (classNameType == 10 || classNameType == 11)
                unknown = dataStream.ReadValueU8();
            else
            {
                childrenCount = dataStream.ReadValueU8();
                unknownA = dataStream.ReadValueU8();
                unknownB = dataStream.ReadValueU8();
            }

            byte childrenCountF = childrenCount;

            sbyte offset = 0;
            if (isNewDawn)
                offset = 4;
            if (isFC6)
            {
                //offset = 5;
            }
            // each class type has own binary structure
            Console.WriteLine("classNameType: " + classNameType);
            Console.WriteLine("offset: " + offset);
            if (classNameType == 7)
            {
                endSpace = 2;
                CMoveState();
            }
            else if (classNameType == 8)
            {
                endSpace = 2;
                CMoveBranch();
            }
            else if (classNameType == 9)
            {
                endSpace = 2;
                CMoveBracket();
            }
            else if (classNameType == 10)
                CMoveComparisonOpe();
            else if (classNameType == 11)
                CMoveIntervalOpe();
            else if (classNameType == 12)
            {
                childrenCountF = unknownA;
                unknownA = childrenCount;
                CMoveBlendRef();
            }
            else if (classNameType == 13)
            {
                childrenCountF = unknownA;
                unknownA = childrenCount;
                CMoveMultiBlendRef();
            }
            else if (classNameType == 14)
            {
                childrenCountF = unknownA;
                unknownA = childrenCount;
                CMoveSuspendLayer();
            }
            else if (classNameType == 15)
                CMoveStateRef();
            else if (classNameType == 16)
                CMoveSingleAnim();
            else if (classNameType == 17)
                CMoveSpeedScaledAnim();
            else if (classNameType == 18)
                CMoveRandomOffsetAnim();
            else if (classNameType == 19)
                CMoveTimeControlledAnim();
            else if (classNameType == 20)
                CMoveDoNothing();
            else if (classNameType == 21)
                CMoveMotionMatching();
            else if (classNameType == 22 && (isNewDawn || isFC6))
                CMoveMotionMatchingList();
            else if (classNameType == 23 && (isNewDawn || isFC6))
                CMoveMotionMatchingAnim();
            else if (classNameType == 24 && (isNewDawn || isFC6))
                CMoveMotionMatchingPartialAnim();
            else if (classNameType == 25 && (isNewDawn || isFC6))
                CMoveMotionMatchingAnimGroup();
            else if (classNameType == 22 + offset || (classNameType == 26 && isFC6))
                CMoveFacialAnim();
            else if (classNameType == 24 + offset || (classNameType == 28 && isFC6))
                CMoveProcedural();
            else if (classNameType == 26 + offset || (classNameType == 30 && isFC6))
                CMoveRandomSelector();
            else if (classNameType == 27 + offset || (classNameType == 31 && isFC6))
                CMovePMSSelector();
            else if (classNameType == 28 + offset || (classNameType == 32 && isFC6))
                CMoveSequence();
            else if (classNameType == 29 + offset || (classNameType == 33 && isFC6))
                CMoveRangeBlend();
            else if (classNameType == 30 + offset || (classNameType == 34 && isFC6))
                CMoveAxialBlend();
            else if (classNameType == 32 + offset || (classNameType == 36 && isFC6))
                CMoveMultiBlend();
            else if (classNameType == 35 + offset || (classNameType == 39 && isFC6))
                CMoveSetGameParams();
            else if (classNameType == 36 + offset || (classNameType == 41 && isFC6))
                CMoveAnimTechSetPMS();
            else if (classNameType == 37 + offset)
                CMoveAnimTechProgressToPMS();
            else if (classNameType == 38 + offset || (classNameType == 43 && isFC6))
                CMoveAnimTechAnchor();
            else if (classNameType == 39 + offset || (classNameType == 44 && isFC6))
                CMoveAnimTechIKPath();
            else if (classNameType == 41 + offset || (classNameType == 46 && isFC6))
                CMoveTransition();
            else if (classNameType == 42 + offset || (classNameType == 47 && isFC6))
            {
                endSpace = 1;
                childrenCountF = unknownA;
                unknownA = childrenCount;
                CMoveTransitionContainer();
            }
            else
            {
                writer.Flush();
                Console.WriteLine("Missing class name type " + classNameType + " in template! Skipping.");
            }

            if (classNameType == 10 || classNameType == 11)
                writer.WriteAttributeString("headerValue", unknown.ToString());
            else
            {
                Console.WriteLine("childrenCount: " + childrenCount);
                Console.WriteLine("unknownA: " + unknownA);
                Console.WriteLine("unknownB: " + unknownB);
                writer.WriteAttributeString("headerValueA", unknownA.ToString());
                writer.WriteAttributeString("headerValueB", unknownB.ToString());
            }

            if (childrenCountF > 0)
            {
                List<ushort> childPos = new List<ushort>();
                for (int i = 0; i < childrenCountF; i++)
                {
                    ushort pos = dataStream.ReadValueU16();
                    Console.WriteLine("Child pos: " + pos);
                    if (pos != 0)
                    {
                        childPos.Add(pos); //Position of child from begin * 4. Size with ending must be 8 bytes.
                    }
                }
                int padding = (4 - ((childrenCountF - endSpace) % 4)) % 4;
                dataStream.ReadBytes(padding * sizeof(short));
                Console.WriteLine("Total padding: " + padding * sizeof(short));
                long currentPos, seekPos;
                for (int i = 0; i < childrenCountF; i++)
                {
                    currentPos = dataStream.Position;
                    seekPos = childPos[i] * 4;
                    dataStream.Seek(seekPos, SeekOrigin.Begin);
                    Console.WriteLine("Deserializing (currentPos, seekPos): " + currentPos + " " + seekPos);
                    Console.WriteLine("Child pos: " + childPos[i]);
                    ChildDeserialize();

                    dataStream.Seek(currentPos, SeekOrigin.Begin);
                }
            }

            writer.WriteEndElement();
        }

        //******************************************************************************

        public byte[] Serialize(XPathNavigator xPathNavigator)
        {
            dataStream = new MemoryStream();

            var root = isCombined ? xPathNavigator : xPathNavigator.SelectSingleNode("CMove_BlendRoot_DTRoot");

            dataStream.WriteValueU64(ulong.Parse(root.GetAttribute("hash", ""), NumberStyles.HexNumber));

            var childs = root.SelectChildren(XPathNodeType.Element);
            while (childs.MoveNext() == true)
                ChildSerialize(childs.Current);

            long size = dataStream.Position;

            dataStream.Seek(0, SeekOrigin.Begin);
            var memoryStream = new MemoryStream();
            dataStream.CopyTo(memoryStream);

            if (IncludedLength)
            {
                List<byte[]> output = new List<byte[]>
                {
                    BitConverter.GetBytes((uint)size),
                    memoryStream.ToArray()
                };

                return output.SelectMany(byteArr => byteArr).ToArray();
            }

            if (isCombined)
                PerMoveResourceInfo.perMoveResourceInfos.Add(new PerMoveResourceInfoItem((uint)size, ulong.Parse(root.GetAttribute("rootNodeId", ""), NumberStyles.HexNumber), ulong.Parse(root.GetAttribute("hash", ""), NumberStyles.HexNumber)));

            return memoryStream.ToArray();
        }

        private void ChildSerialize(XPathNavigator root)
        {
            int endSpace = 0;

            var childs = root.SelectChildren(XPathNodeType.Element);
            byte childCount = (byte)childs.Count;

            byte.TryParse(root.GetAttribute("headerValue", ""), out byte unknown);
            byte.TryParse(root.GetAttribute("headerValueA", ""), out byte unknownHA);
            byte.TryParse(root.GetAttribute("headerValueB", ""), out byte unknownHB);

            sbyte offset = 0;
            if (isNewDawn)
                offset = 4;

            switch (root.Name)
            {
                case "CMoveAxialBlend":
                    WriteChildHeader(30 + offset, childCount, unknownHA, unknownHB);
                    CMoveAxialBlend(root);
                    break;
                case "CMoveSingleAnim":
                    WriteChildHeader(16, childCount, unknownHA, unknownHB);
                    CMoveSingleAnim(root);
                    break;
                case "CMoveAnimTechAnchor":
                    WriteChildHeader(38 + offset, childCount, unknownHA, unknownHB);
                    CMoveAnimTechAnchor(root);
                    break;
                case "CMoveSetGameParams":
                    WriteChildHeader(35 + offset, childCount, unknownHA, unknownHB);
                    CMoveSetGameParams(root);
                    break;
                case "CMoveAnimTechSetPMS":
                    WriteChildHeader(36 + offset, childCount, unknownHA, unknownHB);
                    CMoveAnimTechSetPMS(root);
                    break;
                case "CMoveAnimTechIKPath":
                    WriteChildHeader(39 + offset, childCount, unknownHA, unknownHB);
                    CMoveAnimTechIKPath(root);
                    break;
                case "CMovePMSSelector":
                    WriteChildHeader(27 + offset, childCount, unknownHA, unknownHB);
                    CMovePMSSelector(root);
                    break;
                case "CMoveTimeControlledAnim":
                    WriteChildHeader(19, childCount, unknownHA, unknownHB);
                    CMoveTimeControlledAnim(root);
                    break;
                case "CMoveDoNothing":
                    WriteChildHeader(20, childCount, unknownHA, unknownHB);
                    CMoveDoNothing(root);
                    break;
                case "CMoveRangeBlend":
                    WriteChildHeader(29 + offset, childCount, unknownHA, unknownHB);
                    CMoveRangeBlend(root);
                    break;
                case "CMoveSpeedScaledAnim":
                    WriteChildHeader(17, childCount, unknownHA, unknownHB);
                    CMoveSpeedScaledAnim(root);
                    break;
                case "CMoveState":
                    endSpace = 2;
                    WriteChildHeader(7, childCount, unknownHA, unknownHB);
                    CMoveState(root);
                    break;
                case "CMoveBlendRef":
                    WriteChildHeader(12, unknownHA, childCount, unknownHB);
                    CMoveBlendRef(root);
                    break;
                case "CMoveBranch":
                    endSpace = 2;
                    WriteChildHeader(8, childCount, unknownHA, unknownHB);
                    CMoveBranch(root);
                    break;
                case "CMoveMultiBlendRef":
                    WriteChildHeader(13, unknownHA, childCount, unknownHB);
                    CMoveMultiBlendRef(root);
                    break;
                case "CMoveBracket":
                    endSpace = 2;
                    WriteChildHeader(9, childCount, unknownHA, unknownHB);
                    CMoveBracket(root);
                    break;
                case "CMoveComparisonOpe":
                    WriteChildHeader(10, unknown);
                    CMoveComparisonOpe(root);
                    break;
                case "CMoveIntervalOpe":
                    WriteChildHeader(11, unknown);
                    CMoveIntervalOpe(root);
                    break;
                case "CMoveMultiBlend":
                    WriteChildHeader(32 + offset, childCount, unknownHA, unknownHB);
                    CMoveMultiBlend(root);
                    break;
                case "CMoveRandomSelector":
                    WriteChildHeader(26 + offset, childCount, unknownHA, unknownHB);
                    CMoveRandomSelector(root);
                    break;
                case "CMoveTransition":
                    WriteChildHeader(41 + offset, childCount, unknownHA, unknownHB);
                    CMoveTransition(root);
                    break;
                case "CMoveTransitionContainer":
                    endSpace = 1;
                    WriteChildHeader(42 + offset, unknownHA, childCount, unknownHB);
                    CMoveTransitionContainer(root);
                    break;
                case "CMoveStateRef":
                    WriteChildHeader(15, childCount, unknownHA, unknownHB);
                    CMoveStateRef(root);
                    break;
                case "CMoveSuspendLayer":
                    WriteChildHeader(14, unknownHA, childCount, unknownHB);
                    CMoveSuspendLayer(root);
                    break;
                case "CMoveRandomOffsetAnim":
                    WriteChildHeader(18, childCount, unknownHA, unknownHB);
                    CMoveRandomOffsetAnim(root);
                    break;
                case "CMoveProcedural":
                    WriteChildHeader(24 + offset, childCount, unknownHA, unknownHB);
                    CMoveProcedural(root);
                    break;
                case "CMoveMotionMatching":
                    WriteChildHeader(21, childCount, unknownHA, unknownHB);
                    CMoveMotionMatching(root);
                    break;
                case "CMoveFacialAnim":
                    WriteChildHeader(22 + offset, childCount, unknownHA, unknownHB);
                    CMoveFacialAnim(root);
                    break;
                case "CMoveSequence":
                    WriteChildHeader(28 + offset, childCount, unknownHA, unknownHB);
                    CMoveSequence(root);
                    break;
                case "CMoveAnimTechProgressToPMS":
                    WriteChildHeader(37 + offset, childCount, unknownHA, unknownHB);
                    CMoveAnimTechProgressToPMS(root);
                    break;
                case "CMoveMotionMatchingList":
                    WriteChildHeader(22, childCount, unknownHA, unknownHB);
                    CMoveMotionMatchingList(root);
                    break;
                case "CMoveMotionMatchingPartialAnim":
                    WriteChildHeader(24, childCount, unknownHA, unknownHB);
                    CMoveMotionMatchingPartialAnim(root);
                    break;
                case "CMoveMotionMatchingAnim":
                    WriteChildHeader(23, childCount, unknownHA, unknownHB);
                    CMoveMotionMatchingAnim(root);
                    break;
                case "CMoveMotionMatchingAnimGroup":
                    WriteChildHeader(25, childCount, unknownHA, unknownHB);
                    CMoveMotionMatchingAnimGroup(root);
                    break;
                default:
                    Console.WriteLine("Missing class name type " + root.Name + " in template! Exiting.");
                    return;
            }

            long posBeforeChildsPos = dataStream.Position;

            int padding = (4 - ((childCount - endSpace) % 4)) % 4;

            for (int i = 0; i < childCount + padding; i++)
                dataStream.WriteValueU16(0, Endian.Little);

            List<long> childsPos = new List<long>();
            if (childCount > 0)
            {
                while (childs.MoveNext() == true)
                {
                    childsPos.Add(dataStream.Position);
                    ChildSerialize(childs.Current);
                }
            }

            long currentPos = dataStream.Position;
            dataStream.Seek(posBeforeChildsPos, SeekOrigin.Begin);

            for (int i = 0; i < childCount; i++)
                dataStream.WriteValueU16((ushort)(childsPos[i] / 4), Endian.Little);

            dataStream.Seek(currentPos, SeekOrigin.Begin);
        }

        private void WriteChildHeader(int classType, byte childCount, byte unknownHA, byte unknownHB)
        {
            dataStream.WriteValueS8((sbyte)classType);
            dataStream.WriteValueU8(childCount);
            dataStream.WriteValueU8(unknownHA);
            dataStream.WriteValueU8(unknownHB);
        }

        private void WriteChildHeader(int classType, byte unknown)
        {
            dataStream.WriteValueS8((sbyte)classType);
            dataStream.WriteValueU8(unknown);
        }

        //******************************************************************************

        private void CMoveAxialBlend(XPathNavigator xmlNav = null)
        {
            if (xmlNav == null)
            {
                writer.WriteStartElement("CMoveAxialBlend");
                writer.WriteAttributeString("space0", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("uniqueID", dataStream.ReadValueU64().ToString());
                writer.WriteAttributeString("CriteriaValue", dataStream.ReadValueF32().ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("space1", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("layerParamOffset", GetHashFromOffset());
                writer.WriteAttributeString("space2", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("unknownA", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("space3", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("space4", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("pmsvalueParamOffset", GetHashFromOffset(true));
                writer.WriteAttributeString("unknownB", dataStream.ReadValueS16().ToString());
            }
            else
            {
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space0", "")));
                dataStream.WriteValueU64(ulong.Parse(xmlNav.GetAttribute("uniqueID", "")));
                dataStream.WriteValueF32(float.Parse(xmlNav.GetAttribute("CriteriaValue", ""), CultureInfo.InvariantCulture));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space1", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("layerParamOffset", ""), OffsetsHashesArray.ParamNames.LAYERPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space2", "")));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("unknownA", "")));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space3", "")));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space4", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("pmsvalueParamOffset", ""), OffsetsHashesArray.ParamNames.PMSVALUEPARAM, true);
                dataStream.WriteValueS16(short.Parse(xmlNav.GetAttribute("unknownB", "")));
            }
        }

        private void CMoveSingleAnim(XPathNavigator xmlNav = null)
        {
            if (xmlNav == null)
            {
                writer.WriteStartElement("CMoveSingleAnim");
                writer.WriteAttributeString("space0", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("uniqueID", dataStream.ReadValueU64().ToString());
                writer.WriteAttributeString("CriteriaValue", dataStream.ReadValueF32().ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("space1", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("layerParamOffset", GetHashFromOffset());
                writer.WriteAttributeString("space2", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("unknownA", dataStream.ReadValueS16().ToString());
                writer.WriteAttributeString("unknownB", dataStream.ReadValueS16().ToString());
                writer.WriteAttributeString("space3", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("blendParamOffset", GetHashFromOffset());
                writer.WriteAttributeString("space4", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("clipParamOffset", GetHashFromOffset());
                writer.WriteAttributeString("space5", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("animParamOffset", GetHashFromOffset());
                writer.WriteAttributeString("space6", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("ragdollParamOffset", GetHashFromOffset());
                writer.WriteAttributeString("space7", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("lookatParamOffset", GetHashFromOffset());
                writer.WriteAttributeString("space8", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("secondarymotionParamOffset", GetHashFromOffset());
                writer.WriteAttributeString("space9", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("blendadjustParamOffset", GetHashFromOffset());
                writer.WriteAttributeString("space10", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("unknownC", dataStream.ReadValueU16().ToString());
                writer.WriteAttributeString("pmsvalueParamOffset1", GetHashFromOffset(true));
                writer.WriteAttributeString("unknownE", dataStream.ReadValueF32().ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("unknownF", dataStream.ReadValueU16().ToString());
                writer.WriteAttributeString("pmsvalueParamOffset2", GetHashFromOffset(true));
                writer.WriteAttributeString("space11", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("unknownH", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("space12", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("space13", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("space14", dataStream.ReadValueS32().ToString());
            }
            else
            {
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space0", "")));
                dataStream.WriteValueU64(ulong.Parse(xmlNav.GetAttribute("uniqueID", "")));
                dataStream.WriteValueF32(float.Parse(xmlNav.GetAttribute("CriteriaValue", ""), CultureInfo.InvariantCulture));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space1", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("layerParamOffset", ""), OffsetsHashesArray.ParamNames.LAYERPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space2", "")));
                dataStream.WriteValueS16(short.Parse(xmlNav.GetAttribute("unknownA", "")));
                dataStream.WriteValueS16(short.Parse(xmlNav.GetAttribute("unknownB", "")));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space3", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("blendParamOffset", ""), OffsetsHashesArray.ParamNames.BLENDPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space4", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("clipParamOffset", ""), OffsetsHashesArray.ParamNames.CLIPPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space5", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("animParamOffset", ""), OffsetsHashesArray.ParamNames.ANIMPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space6", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("ragdollParamOffset", ""), OffsetsHashesArray.ParamNames.RAGDOLLPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space7", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("lookatParamOffset", ""), OffsetsHashesArray.ParamNames.LOOAKATPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space8", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("secondarymotionParamOffset", ""), OffsetsHashesArray.ParamNames.SECONDARYMOTIONPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space9", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("blendadjustParamOffset", ""), OffsetsHashesArray.ParamNames.BLENDADJUSTPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space10", "")));
                dataStream.WriteValueU16(ushort.Parse(xmlNav.GetAttribute("unknownC", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("pmsvalueParamOffset1", ""), OffsetsHashesArray.ParamNames.PMSVALUEPARAM, true);
                dataStream.WriteValueF32(float.Parse(xmlNav.GetAttribute("unknownE", ""), CultureInfo.InvariantCulture));
                dataStream.WriteValueU16(ushort.Parse(xmlNav.GetAttribute("unknownF", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("pmsvalueParamOffset2", ""), OffsetsHashesArray.ParamNames.PMSVALUEPARAM, true);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space11", "")));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("unknownH", "")));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space12", "")));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space13", "")));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space14", "")));
            }
        }

        private void CMoveAnimTechAnchor(XPathNavigator xmlNav = null)
        {
            if (xmlNav == null)
            {
                writer.WriteStartElement("CMoveAnimTechAnchor");
                writer.WriteAttributeString("space0", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("unknownA", dataStream.ReadValueF32().ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("space1", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("blendParamOffset1", GetHashFromOffset());
                writer.WriteAttributeString("space2", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("blendParamOffset2", GetHashFromOffset());
                writer.WriteAttributeString("space3", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("curveParamOffset", GetHashFromOffset());
                writer.WriteAttributeString("space4", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("PartID", dataStream.ReadValueU32().ToString());
                writer.WriteAttributeString("ParentID", dataStream.ReadValueU32().ToString());
            }
            else
            {
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space0", "")));
                dataStream.WriteValueF32(float.Parse(xmlNav.GetAttribute("unknownA", ""), CultureInfo.InvariantCulture));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space1", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("blendParamOffset1", ""), OffsetsHashesArray.ParamNames.BLENDPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space2", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("blendParamOffset2", ""), OffsetsHashesArray.ParamNames.BLENDPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space3", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("curveParamOffset", ""), OffsetsHashesArray.ParamNames.CURVEPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space4", "")));
                dataStream.WriteValueU32(uint.Parse(xmlNav.GetAttribute("PartID", "")));
                dataStream.WriteValueU32(uint.Parse(xmlNav.GetAttribute("ParentID", "")));
            }
        }

        private void CMoveSetGameParams(XPathNavigator xmlNav = null)
        {
            if (xmlNav == null)
            {
                writer.WriteStartElement("CMoveSetGameParams");
                writer.WriteAttributeString("CriteriaValue", dataStream.ReadValueF32().ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("hash_83E5E874", dataStream.ReadValueS16().ToString());
                writer.WriteAttributeString("MotionOrientationCorrection", dataStream.ReadValueU8().ToString());
                writer.WriteAttributeString("unknownA", dataStream.ReadValueU8().ToString());
                writer.WriteAttributeString("space0", dataStream.ReadValueS32().ToString());
            }
            else
            {
                dataStream.WriteValueF32(float.Parse(xmlNav.GetAttribute("CriteriaValue", ""), CultureInfo.InvariantCulture));
                dataStream.WriteValueS16(short.Parse(xmlNav.GetAttribute("hash_83E5E874", "")));
                dataStream.WriteValueU8(byte.Parse(xmlNav.GetAttribute("MotionOrientationCorrection", "")));
                dataStream.WriteValueU8(byte.Parse(xmlNav.GetAttribute("unknownA", "")));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space0", "")));
            }
        }

        private void CMoveAnimTechSetPMS(XPathNavigator xmlNav = null)
        {
            if (xmlNav == null)
            {
                writer.WriteStartElement("CMoveAnimTechSetPMS");
                writer.WriteAttributeString("space0", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("unknownA", dataStream.ReadValueF32().ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("space1", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("blendParamOffset1", GetHashFromOffset());
                writer.WriteAttributeString("space2", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("blendParamOffset2", GetHashFromOffset());
                writer.WriteAttributeString("space3", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("curveParamOffset", GetHashFromOffset());
                writer.WriteAttributeString("space4", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("pmsvalueParamOffset", GetHashFromOffset(true));
                writer.WriteAttributeString("unknownC", dataStream.ReadValueU16().ToString());
                writer.WriteAttributeString("CriteriaValue", dataStream.ReadValueF32().ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space0", "")));
                dataStream.WriteValueF32(float.Parse(xmlNav.GetAttribute("unknownA", ""), CultureInfo.InvariantCulture));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space1", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("blendParamOffset1", ""), OffsetsHashesArray.ParamNames.BLENDPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space2", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("blendParamOffset2", ""), OffsetsHashesArray.ParamNames.BLENDPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space3", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("curveParamOffset", ""), OffsetsHashesArray.ParamNames.CURVEPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space4", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("pmsvalueParamOffset", ""), OffsetsHashesArray.ParamNames.PMSVALUEPARAM, true);
                dataStream.WriteValueU16(ushort.Parse(xmlNav.GetAttribute("unknownC", "")));
                dataStream.WriteValueF32(float.Parse(xmlNav.GetAttribute("CriteriaValue", ""), CultureInfo.InvariantCulture));
            }
        }

        private void CMoveAnimTechIKPath(XPathNavigator xmlNav = null)
        {
            if (xmlNav == null)
            {
                writer.WriteStartElement("CMoveAnimTechIKPath");
                writer.WriteAttributeString("space0", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("unknownA", dataStream.ReadValueF32().ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("space1", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("blendParamOffset1", GetHashFromOffset());
                writer.WriteAttributeString("space2", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("blendParamOffset2", GetHashFromOffset());
                writer.WriteAttributeString("space3", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("curveParamOffset", GetHashFromOffset());
                writer.WriteAttributeString("space4", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("PartID", dataStream.ReadValueU32().ToString());
                writer.WriteAttributeString("ParentID", dataStream.ReadValueU32().ToString());
                writer.WriteAttributeString("hash_4FB3ADB0", dataStream.ReadValueU32().ToString());
                writer.WriteAttributeString("TargetBoneID", dataStream.ReadValueU32().ToString());
                writer.WriteAttributeString("pmsvalueParamOffset", GetHashFromOffset(true));
                writer.WriteAttributeString("unknownE", dataStream.ReadValueS16().ToString());
                writer.WriteAttributeString("space5", dataStream.ReadValueS32().ToString());
            }
            else
            {
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space0", "")));
                dataStream.WriteValueF32(float.Parse(xmlNav.GetAttribute("unknownA", ""), CultureInfo.InvariantCulture));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space1", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("blendParamOffset1", ""), OffsetsHashesArray.ParamNames.BLENDPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space2", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("blendParamOffset2", ""), OffsetsHashesArray.ParamNames.BLENDPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space3", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("curveParamOffset", ""), OffsetsHashesArray.ParamNames.CURVEPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space4", "")));
                dataStream.WriteValueU32(uint.Parse(xmlNav.GetAttribute("PartID", "")));
                dataStream.WriteValueU32(uint.Parse(xmlNav.GetAttribute("ParentID", "")));
                dataStream.WriteValueU32(uint.Parse(xmlNav.GetAttribute("hash_4FB3ADB0", "")));
                dataStream.WriteValueU32(uint.Parse(xmlNav.GetAttribute("TargetBoneID", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("pmsvalueParamOffset", ""), OffsetsHashesArray.ParamNames.PMSVALUEPARAM, true);
                dataStream.WriteValueS16(short.Parse(xmlNav.GetAttribute("unknownE", "")));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space5", "")));
            }
        }

        private void CMovePMSSelector(XPathNavigator xmlNav = null)
        {
            if (xmlNav == null)
            {
                int range = 168;
                if (isFC6)
                {
                    // FC6 has extra padding
                    range = 208;
                }
                writer.WriteStartElement("CMovePMSSelector");
                writer.WriteAttributeString("space0", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("uniqueID", dataStream.ReadValueU64().ToString());
                writer.WriteAttributeString("pmsTargetValue", dataStream.ReadValueF32().ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("space1", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("layerParamOffset", GetHashFromOffset());
                writer.WriteAttributeString("space2", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("unknownA", dataStream.ReadValueS16().ToString());
                writer.WriteAttributeString("unknownB", dataStream.ReadValueS16().ToString());
                writer.WriteAttributeString("space3", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("pmsvalueParamOffset", GetHashFromOffset(true));
                writer.WriteAttributeString("unknownC", dataStream.ReadValueS16().ToString());
                writer.WriteAttributeString("CriteriaValue_86E56C10", dataStream.ReadValueF32().ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("ranges", Helpers.ByteArrayToString(dataStream.ReadBytes(range)));
            }
            else
            {
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space0", "")));
                dataStream.WriteValueU64(ulong.Parse(xmlNav.GetAttribute("uniqueID", "")));
                dataStream.WriteValueF32(float.Parse(xmlNav.GetAttribute("pmsTargetValue", ""), CultureInfo.InvariantCulture));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space1", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("layerParamOffset", ""), OffsetsHashesArray.ParamNames.LAYERPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space2", "")));
                dataStream.WriteValueS16(short.Parse(xmlNav.GetAttribute("unknownA", "")));
                dataStream.WriteValueS16(short.Parse(xmlNav.GetAttribute("unknownB", "")));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space3", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("pmsvalueParamOffset", ""), OffsetsHashesArray.ParamNames.PMSVALUEPARAM, true);
                dataStream.WriteValueS16(short.Parse(xmlNav.GetAttribute("unknownC", "")));
                dataStream.WriteValueF32(float.Parse(xmlNav.GetAttribute("CriteriaValue_86E56C10", ""), CultureInfo.InvariantCulture));
                dataStream.WriteBytes(Helpers.StringToByteArray(xmlNav.GetAttribute("ranges", "")));
            }
        }

        private void CMoveTimeControlledAnim(XPathNavigator xmlNav = null)
        {
            if (xmlNav == null)
            {
                writer.WriteStartElement("CMoveTimeControlledAnim");
                writer.WriteAttributeString("space0", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("uniqueID", dataStream.ReadValueU64().ToString());
                writer.WriteAttributeString("CriteriaValue", dataStream.ReadValueF32().ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("space1", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("layerParamOffset", GetHashFromOffset());
                writer.WriteAttributeString("space2", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("unknownA", dataStream.ReadValueS16().ToString());
                writer.WriteAttributeString("unknownB", dataStream.ReadValueS16().ToString());
                writer.WriteAttributeString("space3", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("blendParamOffset", GetHashFromOffset());
                writer.WriteAttributeString("space4", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("clipParamOffset", GetHashFromOffset());
                writer.WriteAttributeString("space5", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("animParamOffset", GetHashFromOffset());
                writer.WriteAttributeString("space6", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("ragdollParamOffset", GetHashFromOffset());
                writer.WriteAttributeString("space7", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("lookatParamOffset", GetHashFromOffset());
                writer.WriteAttributeString("space8", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("secondarymotionParamOffset", GetHashFromOffset());
                writer.WriteAttributeString("space9", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("blendadjustParamOffset", GetHashFromOffset());
                writer.WriteAttributeString("space10", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("unknownC", dataStream.ReadValueU16().ToString());
                writer.WriteAttributeString("pmsvalueParamOffset1", GetHashFromOffset(true));
                writer.WriteAttributeString("unknownE", dataStream.ReadValueF32().ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("unknownF", dataStream.ReadValueU16().ToString());
                writer.WriteAttributeString("pmsvalueParamOffset2", GetHashFromOffset(true));
                writer.WriteAttributeString("space11", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("unknownH", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("space12", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("space13", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("space14", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("space15", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("unknownI", dataStream.ReadValueF32().ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("pmsvalueParamOffset3", GetHashFromOffset(true));
                writer.WriteAttributeString("unknownK", dataStream.ReadValueU16().ToString());
                writer.WriteAttributeString("space16", dataStream.ReadValueS32().ToString());
            }
            else
            {
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space0", "")));
                dataStream.WriteValueU64(ulong.Parse(xmlNav.GetAttribute("uniqueID", "")));
                dataStream.WriteValueF32(float.Parse(xmlNav.GetAttribute("CriteriaValue", ""), CultureInfo.InvariantCulture));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space1", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("layerParamOffset", ""), OffsetsHashesArray.ParamNames.LAYERPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space2", "")));
                dataStream.WriteValueS16(short.Parse(xmlNav.GetAttribute("unknownA", "")));
                dataStream.WriteValueS16(short.Parse(xmlNav.GetAttribute("unknownB", "")));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space3", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("blendParamOffset", ""), OffsetsHashesArray.ParamNames.BLENDPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space4", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("clipParamOffset", ""), OffsetsHashesArray.ParamNames.CLIPPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space5", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("animParamOffset", ""), OffsetsHashesArray.ParamNames.ANIMPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space6", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("ragdollParamOffset", ""), OffsetsHashesArray.ParamNames.RAGDOLLPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space7", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("lookatParamOffset", ""), OffsetsHashesArray.ParamNames.LOOAKATPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space8", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("secondarymotionParamOffset", ""), OffsetsHashesArray.ParamNames.SECONDARYMOTIONPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space9", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("blendadjustParamOffset", ""), OffsetsHashesArray.ParamNames.BLENDADJUSTPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space10", "")));
                dataStream.WriteValueU16(ushort.Parse(xmlNav.GetAttribute("unknownC", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("pmsvalueParamOffset1", ""), OffsetsHashesArray.ParamNames.PMSVALUEPARAM, true);
                dataStream.WriteValueF32(float.Parse(xmlNav.GetAttribute("unknownE", ""), CultureInfo.InvariantCulture));
                dataStream.WriteValueU16(ushort.Parse(xmlNav.GetAttribute("unknownF", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("pmsvalueParamOffset2", ""), OffsetsHashesArray.ParamNames.PMSVALUEPARAM, true);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space11", "")));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("unknownH", "")));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space12", "")));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space13", "")));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space14", "")));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space15", "")));
                dataStream.WriteValueF32(float.Parse(xmlNav.GetAttribute("unknownI", ""), CultureInfo.InvariantCulture));
                GetOffsetFromHash(xmlNav.GetAttribute("pmsvalueParamOffset3", ""), OffsetsHashesArray.ParamNames.PMSVALUEPARAM, true);
                dataStream.WriteValueU16(ushort.Parse(xmlNav.GetAttribute("unknownK", "")));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space16", "")));
            }
        }

        private void CMoveDoNothing(XPathNavigator xmlNav = null)
        {
            if (xmlNav == null)
            {
                writer.WriteStartElement("CMoveDoNothing");
                writer.WriteAttributeString("space0", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("uniqueID", dataStream.ReadValueU64().ToString());
                writer.WriteAttributeString("CriteriaValue", dataStream.ReadValueF32().ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("space1", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("layerParamOffset", GetHashFromOffset());
                writer.WriteAttributeString("space2", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("unknownA", dataStream.ReadValueS16().ToString());
                writer.WriteAttributeString("unknownB", dataStream.ReadValueS16().ToString());
                writer.WriteAttributeString("space3", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("blendParamOffset", GetHashFromOffset());
                writer.WriteAttributeString("space4", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("clipParamOffset", GetHashFromOffset());
                writer.WriteAttributeString("space5", dataStream.ReadValueS32().ToString());
            }
            else
            {
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space0", "")));
                dataStream.WriteValueU64(ulong.Parse(xmlNav.GetAttribute("uniqueID", "")));
                dataStream.WriteValueF32(float.Parse(xmlNav.GetAttribute("CriteriaValue", ""), CultureInfo.InvariantCulture));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space1", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("layerParamOffset", ""), OffsetsHashesArray.ParamNames.LAYERPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space2", "")));
                dataStream.WriteValueS16(short.Parse(xmlNav.GetAttribute("unknownA", "")));
                dataStream.WriteValueS16(short.Parse(xmlNav.GetAttribute("unknownB", "")));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space3", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("blendParamOffset", ""), OffsetsHashesArray.ParamNames.BLENDPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space4", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("clipParamOffset", ""), OffsetsHashesArray.ParamNames.CLIPPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space5", "")));
            }
        }

        private void CMoveRangeBlend(XPathNavigator xmlNav = null)
        {
            if (xmlNav == null)
            {
                writer.WriteStartElement("CMoveRangeBlend");
                writer.WriteAttributeString("space0", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("uniqueID", dataStream.ReadValueU64().ToString());
                writer.WriteAttributeString("CriteriaValue", dataStream.ReadValueF32().ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("space1", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("layerParamOffset", GetHashFromOffset());
                writer.WriteAttributeString("space2", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("unknownA", dataStream.ReadValueS16().ToString());
                writer.WriteAttributeString("unknownB", dataStream.ReadValueS16().ToString());
                writer.WriteAttributeString("space3", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("space4", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("pmsvalueParamOffset", GetHashFromOffset(true));
                writer.WriteAttributeString("unknownC", dataStream.ReadValueS16().ToString());
                writer.WriteAttributeString("ranges", Helpers.ByteArrayToString(dataStream.ReadBytes(168)));
            }
            else
            {
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space0", "")));
                dataStream.WriteValueU64(ulong.Parse(xmlNav.GetAttribute("uniqueID", "")));
                dataStream.WriteValueF32(float.Parse(xmlNav.GetAttribute("CriteriaValue", ""), CultureInfo.InvariantCulture));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space1", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("layerParamOffset", ""), OffsetsHashesArray.ParamNames.LAYERPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space2", "")));
                dataStream.WriteValueS16(short.Parse(xmlNav.GetAttribute("unknownA", "")));
                dataStream.WriteValueS16(short.Parse(xmlNav.GetAttribute("unknownB", "")));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space3", "")));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space4", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("pmsvalueParamOffset", ""), OffsetsHashesArray.ParamNames.PMSVALUEPARAM, true);
                dataStream.WriteValueS16(short.Parse(xmlNav.GetAttribute("unknownC", "")));
                dataStream.WriteBytes(Helpers.StringToByteArray(xmlNav.GetAttribute("ranges", "")));
            }
        }

        private void CMoveSpeedScaledAnim(XPathNavigator xmlNav = null)
        {
            if (xmlNav == null)
            {
                writer.WriteStartElement("CMoveSpeedScaledAnim");
                writer.WriteAttributeString("space0", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("uniqueID", dataStream.ReadValueU64().ToString());
                writer.WriteAttributeString("CriteriaValue", dataStream.ReadValueF32().ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("space1", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("layerParamOffset", GetHashFromOffset());
                writer.WriteAttributeString("space2", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("unknownA", dataStream.ReadValueS16().ToString());
                writer.WriteAttributeString("unknownB", dataStream.ReadValueS16().ToString());
                writer.WriteAttributeString("space3", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("blendParamOffset", GetHashFromOffset());
                writer.WriteAttributeString("space4", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("clipParamOffset", GetHashFromOffset());
                writer.WriteAttributeString("space5", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("animParamOffset", GetHashFromOffset());
                writer.WriteAttributeString("space6", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("ragdollParamOffset", GetHashFromOffset());
                writer.WriteAttributeString("space7", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("lookatParamOffset", GetHashFromOffset());
                writer.WriteAttributeString("space8", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("secondarymotionParamOffset", GetHashFromOffset());
                writer.WriteAttributeString("space9", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("blendadjustParamOffset", GetHashFromOffset());
                writer.WriteAttributeString("space10", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("unknownC", dataStream.ReadValueU16().ToString());
                writer.WriteAttributeString("pmsvalueParamOffset1", GetHashFromOffset(true));
                writer.WriteAttributeString("unknownE", dataStream.ReadValueF32().ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("unknownF", dataStream.ReadValueU16().ToString());
                writer.WriteAttributeString("pmsvalueParamOffset2", GetHashFromOffset(true));
                writer.WriteAttributeString("space11", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("unknownH", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("space12", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("space13", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("space14", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("SpeedScale_hash_A2E017C2", dataStream.ReadValueF32().ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("SpeedScale_hash_9EED289B", dataStream.ReadValueF32().ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("SpeedScale_ScaleReferenceValue", dataStream.ReadValueF32().ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("space15", dataStream.ReadValueS32().ToString());
            }
            else
            {
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space0", "")));
                dataStream.WriteValueU64(ulong.Parse(xmlNav.GetAttribute("uniqueID", "")));
                dataStream.WriteValueF32(float.Parse(xmlNav.GetAttribute("CriteriaValue", ""), CultureInfo.InvariantCulture));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space1", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("layerParamOffset", ""), OffsetsHashesArray.ParamNames.LAYERPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space2", "")));
                dataStream.WriteValueS16(short.Parse(xmlNav.GetAttribute("unknownA", "")));
                dataStream.WriteValueS16(short.Parse(xmlNav.GetAttribute("unknownB", "")));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space3", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("blendParamOffset", ""), OffsetsHashesArray.ParamNames.BLENDPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space4", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("clipParamOffset", ""), OffsetsHashesArray.ParamNames.CLIPPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space5", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("animParamOffset", ""), OffsetsHashesArray.ParamNames.ANIMPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space6", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("ragdollParamOffset", ""), OffsetsHashesArray.ParamNames.RAGDOLLPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space7", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("lookatParamOffset", ""), OffsetsHashesArray.ParamNames.LOOAKATPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space8", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("secondarymotionParamOffset", ""), OffsetsHashesArray.ParamNames.SECONDARYMOTIONPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space9", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("blendadjustParamOffset", ""), OffsetsHashesArray.ParamNames.BLENDADJUSTPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space10", "")));
                dataStream.WriteValueU16(ushort.Parse(xmlNav.GetAttribute("unknownC", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("pmsvalueParamOffset1", ""), OffsetsHashesArray.ParamNames.PMSVALUEPARAM, true);
                dataStream.WriteValueF32(float.Parse(xmlNav.GetAttribute("unknownE", ""), CultureInfo.InvariantCulture));
                dataStream.WriteValueU16(ushort.Parse(xmlNav.GetAttribute("unknownF", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("pmsvalueParamOffset2", ""), OffsetsHashesArray.ParamNames.PMSVALUEPARAM, true);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space11", "")));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("unknownH", "")));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space12", "")));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space13", "")));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space14", "")));
                dataStream.WriteValueF32(float.Parse(xmlNav.GetAttribute("SpeedScale_hash_A2E017C2", ""), CultureInfo.InvariantCulture));
                dataStream.WriteValueF32(float.Parse(xmlNav.GetAttribute("SpeedScale_hash_9EED289B", ""), CultureInfo.InvariantCulture));
                dataStream.WriteValueF32(float.Parse(xmlNav.GetAttribute("SpeedScale_ScaleReferenceValue", ""), CultureInfo.InvariantCulture));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space15", "")));
            }
        }

        private void CMoveState(XPathNavigator xmlNav = null)
        {
            if (xmlNav == null)
            {
                int range = 60;
                if (isFC6)
                {
                    // FC6 has extra padding
                    range = 92;
                }
                writer.WriteStartElement("CMoveState");
                writer.WriteAttributeString("space0", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("ranges", Helpers.ByteArrayToString(dataStream.ReadBytes(range)));
            }
            else
            {
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space0", "")));
                dataStream.WriteBytes(Helpers.StringToByteArray(xmlNav.GetAttribute("ranges", "")));
            }
        }

        private void CMoveBlendRef(XPathNavigator xmlNav = null)
        {
            if (xmlNav == null)
            {
                writer.WriteStartElement("CMoveBlendRef");
                writer.WriteAttributeString("unknownA", dataStream.ReadValueF32().ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("blendParamOffset", GetHashFromOffset());
                writer.WriteAttributeString("space0", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("layerParamOffset", GetHashFromOffset());
                writer.WriteAttributeString("space1", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("moveblendParamOffset", GetHashFromOffset());
                writer.WriteAttributeString("space2", dataStream.ReadValueS32().ToString());
            }
            else
            {
                dataStream.WriteValueF32(float.Parse(xmlNav.GetAttribute("unknownA", ""), CultureInfo.InvariantCulture));
                GetOffsetFromHash(xmlNav.GetAttribute("blendParamOffset", ""), OffsetsHashesArray.ParamNames.BLENDPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space0", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("layerParamOffset", ""), OffsetsHashesArray.ParamNames.LAYERPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space1", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("moveblendParamOffset", ""), OffsetsHashesArray.ParamNames.MOVEBLENDPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space2", "")));
            }
        }

        private void CMoveBranch(XPathNavigator xmlNav = null)
        {
            if (xmlNav == null)
            {
                // is empty

                writer.WriteStartElement("CMoveBranch");
            }
        }

        private void CMoveMultiBlendRef(XPathNavigator xmlNav = null)
        {
            if (xmlNav == null)
            {
                writer.WriteStartElement("CMoveMultiBlendRef");
                writer.WriteAttributeString("unknownA", dataStream.ReadValueF32().ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("blendParamOffset", GetHashFromOffset());
                writer.WriteAttributeString("space0", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("layerParamOffset", GetHashFromOffset());
                writer.WriteAttributeString("space1", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("unknownB", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("space2", dataStream.ReadValueS32().ToString());
            }
            else
            {
                dataStream.WriteValueF32(float.Parse(xmlNav.GetAttribute("unknownA", ""), CultureInfo.InvariantCulture));
                GetOffsetFromHash(xmlNav.GetAttribute("blendParamOffset", ""), OffsetsHashesArray.ParamNames.BLENDPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space0", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("layerParamOffset", ""), OffsetsHashesArray.ParamNames.LAYERPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space1", "")));
                dataStream.WriteValueU32(uint.Parse(xmlNav.GetAttribute("unknownB", "")));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space2", "")));
            }
        }

        private void CMoveBracket(XPathNavigator xmlNav = null)
        {
            if (xmlNav == null)
            {
                // is empty

                writer.WriteStartElement("CMoveBracket");
            }
        }

        private void CMoveComparisonOpe(XPathNavigator xmlNav = null)
        {
            if (xmlNav == null)
            {
                writer.WriteStartElement("CMoveComparisonOpe");
                writer.WriteAttributeString("pmsvalueParamOffset", GetHashFromOffset(true));
                writer.WriteAttributeString("CriteriaValue", dataStream.ReadValueF32().ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                GetOffsetFromHash(xmlNav.GetAttribute("pmsvalueParamOffset", ""), OffsetsHashesArray.ParamNames.PMSVALUEPARAM, true);
                dataStream.WriteValueF32(float.Parse(xmlNav.GetAttribute("CriteriaValue", ""), CultureInfo.InvariantCulture));
            }
        }

        private void CMoveIntervalOpe(XPathNavigator xmlNav = null)
        {
            if (xmlNav == null)
            {
                writer.WriteStartElement("CMoveIntervalOpe");
                writer.WriteAttributeString("pmsvalueParamOffset", GetHashFromOffset(true));
                writer.WriteAttributeString("LowerBound_CriteriaValue", dataStream.ReadValueF32().ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("UpperBound_CriteriaValue", dataStream.ReadValueF32().ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("space", dataStream.ReadValueS32().ToString());
            }
            else
            {
                GetOffsetFromHash(xmlNav.GetAttribute("pmsvalueParamOffset", ""), OffsetsHashesArray.ParamNames.PMSVALUEPARAM, true);
                dataStream.WriteValueF32(float.Parse(xmlNav.GetAttribute("LowerBound_CriteriaValue", ""), CultureInfo.InvariantCulture));
                dataStream.WriteValueF32(float.Parse(xmlNav.GetAttribute("UpperBound_CriteriaValue", ""), CultureInfo.InvariantCulture));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space", "")));
            }
        }

        private void CMoveMultiBlend(XPathNavigator xmlNav = null)
        {
            if (xmlNav == null)
            {
                writer.WriteStartElement("CMoveMultiBlend");
                writer.WriteAttributeString("space0", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("uniqueID", dataStream.ReadValueU64().ToString());
                writer.WriteAttributeString("CriteriaValue", dataStream.ReadValueF32().ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("space1", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("layerParamOffset", GetHashFromOffset());
                writer.WriteAttributeString("space2", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("unknownA", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("space3", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("unknownB", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("space4", dataStream.ReadValueS32().ToString());
            }
            else
            {
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space0", "")));
                dataStream.WriteValueU64(ulong.Parse(xmlNav.GetAttribute("uniqueID", "")));
                dataStream.WriteValueF32(float.Parse(xmlNav.GetAttribute("CriteriaValue", ""), CultureInfo.InvariantCulture));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space1", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("layerParamOffset", ""), OffsetsHashesArray.ParamNames.LAYERPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space2", "")));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("unknownA", "")));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space3", "")));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("unknownB", "")));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space4", "")));
            }
        }

        private void CMoveRandomSelector(XPathNavigator xmlNav = null)
        {
            if (xmlNav == null)
            {
                writer.WriteStartElement("CMoveRandomSelector");
                writer.WriteAttributeString("space0", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("uniqueID", dataStream.ReadValueU64().ToString());
                writer.WriteAttributeString("CriteriaValue", dataStream.ReadValueF32().ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("space1", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("layerParamOffset", GetHashFromOffset());
                writer.WriteAttributeString("space2", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("unknownA", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("space3", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("pmsvalueParamOffset", GetHashFromOffset(true));
                writer.WriteAttributeString("unknownC", dataStream.ReadValueS16().ToString());
                writer.WriteAttributeString("space4", dataStream.ReadValueS32().ToString());
            }
            else
            {
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space0", "")));
                dataStream.WriteValueU64(ulong.Parse(xmlNav.GetAttribute("uniqueID", "")));
                dataStream.WriteValueF32(float.Parse(xmlNav.GetAttribute("CriteriaValue", ""), CultureInfo.InvariantCulture));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space1", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("layerParamOffset", ""), OffsetsHashesArray.ParamNames.LAYERPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space2", "")));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("unknownA", "")));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space3", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("pmsvalueParamOffset", ""), OffsetsHashesArray.ParamNames.PMSVALUEPARAM, true);
                dataStream.WriteValueS16(short.Parse(xmlNav.GetAttribute("unknownC", "")));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space4", "")));
            }
        }

        private void CMoveTransition(XPathNavigator xmlNav = null)
        {
            if (xmlNav == null)
            {
                writer.WriteStartElement("CMoveTransition");
                writer.WriteAttributeString("space0", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("uniqueID", dataStream.ReadValueU64().ToString());
            }
            else
            {
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space0", "")));
                dataStream.WriteValueU64(ulong.Parse(xmlNav.GetAttribute("uniqueID", "")));
            }
        }

        private void CMoveTransitionContainer(XPathNavigator xmlNav = null)
        {
            if (xmlNav == null)
            {
                writer.WriteStartElement("CMoveTransitionContainer");
                writer.WriteAttributeString("unknownA", dataStream.ReadValueS16().ToString());
            }
            else
            {
                dataStream.WriteValueS16(short.Parse(xmlNav.GetAttribute("unknownA", "")));
            }
        }

        private void CMoveStateRef(XPathNavigator xmlNav = null)
        {
            if (xmlNav == null)
            {
                writer.WriteStartElement("CMoveStateRef");
                writer.WriteAttributeString("space0", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("movestateParamOffset", GetHashFromOffset());
                writer.WriteAttributeString("space1", dataStream.ReadValueS32().ToString());
            }
            else
            {
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space0", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("movestateParamOffset", ""), OffsetsHashesArray.ParamNames.MOVESTATEPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space1", "")));
            }
        }

        private void CMoveSuspendLayer(XPathNavigator xmlNav = null)
        {
            if (xmlNav == null)
            {
                writer.WriteStartElement("CMoveSuspendLayer");
                writer.WriteAttributeString("unknownA", dataStream.ReadValueF32().ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("blendParamOffset", GetHashFromOffset());
                writer.WriteAttributeString("space1", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("layerParamOffset", GetHashFromOffset());
                writer.WriteAttributeString("space2", dataStream.ReadValueS32().ToString());
            }
            else
            {
                dataStream.WriteValueF32(float.Parse(xmlNav.GetAttribute("unknownA", ""), CultureInfo.InvariantCulture));
                GetOffsetFromHash(xmlNav.GetAttribute("blendParamOffset", ""), OffsetsHashesArray.ParamNames.BLENDPARAM); // bad ParamNames took me two days finding out where is a problem...
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space1", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("layerParamOffset", ""), OffsetsHashesArray.ParamNames.LAYERPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space2", "")));
            }
        }

        private void CMoveRandomOffsetAnim(XPathNavigator xmlNav = null)
        {
            if (xmlNav == null)
            {
                writer.WriteStartElement("CMoveRandomOffsetAnim");
                writer.WriteAttributeString("space0", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("uniqueID", dataStream.ReadValueU64().ToString());
                writer.WriteAttributeString("CriteriaValue", dataStream.ReadValueF32().ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("space1", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("layerParamOffset", GetHashFromOffset());
                writer.WriteAttributeString("space2", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("unknownA", dataStream.ReadValueS16().ToString());
                writer.WriteAttributeString("unknownB", dataStream.ReadValueS16().ToString());
                writer.WriteAttributeString("space3", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("blendParamOffset", GetHashFromOffset());
                writer.WriteAttributeString("space4", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("clipParamOffset", GetHashFromOffset());
                writer.WriteAttributeString("space5", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("animParamOffset", GetHashFromOffset());
                writer.WriteAttributeString("space6", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("ragdollParamOffset", GetHashFromOffset());
                writer.WriteAttributeString("space7", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("lookatParamOffset", GetHashFromOffset());
                writer.WriteAttributeString("space8", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("secondarymotionParamOffset", GetHashFromOffset());
                writer.WriteAttributeString("space9", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("blendadjustParamOffset", GetHashFromOffset());
                writer.WriteAttributeString("space10", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("unknownC", dataStream.ReadValueU16().ToString());
                writer.WriteAttributeString("pmsvalueParamOffset1", GetHashFromOffset(true));
                writer.WriteAttributeString("unknownE", dataStream.ReadValueF32().ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("unknownF", dataStream.ReadValueU16().ToString());
                writer.WriteAttributeString("pmsvalueParamOffset2", GetHashFromOffset(true));
                writer.WriteAttributeString("space11", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("unknownH", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("space12", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("space13", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("space14", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("RandomOffset_hash_79D9398B", dataStream.ReadValueF32().ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("RandomOffset_hash_DEEDB9F2", dataStream.ReadValueF32().ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("RandomOffset_hash_F2A55EDC", dataStream.ReadValueF32().ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("space15", dataStream.ReadValueS32().ToString());
            }
            else
            {
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space0", "")));
                dataStream.WriteValueU64(ulong.Parse(xmlNav.GetAttribute("uniqueID", "")));
                dataStream.WriteValueF32(float.Parse(xmlNav.GetAttribute("CriteriaValue", ""), CultureInfo.InvariantCulture));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space1", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("layerParamOffset", ""), OffsetsHashesArray.ParamNames.LAYERPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space2", "")));
                dataStream.WriteValueS16(short.Parse(xmlNav.GetAttribute("unknownA", "")));
                dataStream.WriteValueS16(short.Parse(xmlNav.GetAttribute("unknownB", "")));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space3", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("blendParamOffset", ""), OffsetsHashesArray.ParamNames.BLENDPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space4", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("clipParamOffset", ""), OffsetsHashesArray.ParamNames.CLIPPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space5", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("animParamOffset", ""), OffsetsHashesArray.ParamNames.ANIMPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space6", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("ragdollParamOffset", ""), OffsetsHashesArray.ParamNames.RAGDOLLPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space7", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("lookatParamOffset", ""), OffsetsHashesArray.ParamNames.LOOAKATPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space8", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("secondarymotionParamOffset", ""), OffsetsHashesArray.ParamNames.SECONDARYMOTIONPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space9", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("blendadjustParamOffset", ""), OffsetsHashesArray.ParamNames.BLENDADJUSTPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space10", "")));
                dataStream.WriteValueU16(ushort.Parse(xmlNav.GetAttribute("unknownC", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("pmsvalueParamOffset1", ""), OffsetsHashesArray.ParamNames.PMSVALUEPARAM, true);
                dataStream.WriteValueF32(float.Parse(xmlNav.GetAttribute("unknownE", ""), CultureInfo.InvariantCulture));
                dataStream.WriteValueU16(ushort.Parse(xmlNav.GetAttribute("unknownF", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("pmsvalueParamOffset2", ""), OffsetsHashesArray.ParamNames.PMSVALUEPARAM, true);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space11", "")));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("unknownH", "")));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space12", "")));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space13", "")));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space14", "")));
                dataStream.WriteValueF32(float.Parse(xmlNav.GetAttribute("RandomOffset_hash_79D9398B", ""), CultureInfo.InvariantCulture));
                dataStream.WriteValueF32(float.Parse(xmlNav.GetAttribute("RandomOffset_hash_DEEDB9F2", ""), CultureInfo.InvariantCulture));
                dataStream.WriteValueF32(float.Parse(xmlNav.GetAttribute("RandomOffset_hash_F2A55EDC", ""), CultureInfo.InvariantCulture));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space15", "")));
            }
        }

        private void CMoveProcedural(XPathNavigator xmlNav = null)
        {
            if (xmlNav == null)
            {
                writer.WriteStartElement("CMoveProcedural");
                writer.WriteAttributeString("space0", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("uniqueID", dataStream.ReadValueU64().ToString());
                writer.WriteAttributeString("CriteriaValue", dataStream.ReadValueF32().ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("space1", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("layerParamOffset", GetHashFromOffset());
                writer.WriteAttributeString("space2", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("unknownA", dataStream.ReadValueS16().ToString());
                writer.WriteAttributeString("unknownB", dataStream.ReadValueS16().ToString());
                writer.WriteAttributeString("space3", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("blendParamOffset", GetHashFromOffset());
                writer.WriteAttributeString("space4", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("clipParamOffset", GetHashFromOffset());
                writer.WriteAttributeString("space5", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("Context", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("Callback", dataStream.ReadValueS32().ToString());
            }
            else
            {
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space0", "")));
                dataStream.WriteValueU64(ulong.Parse(xmlNav.GetAttribute("uniqueID", "")));
                dataStream.WriteValueF32(float.Parse(xmlNav.GetAttribute("CriteriaValue", ""), CultureInfo.InvariantCulture));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space1", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("layerParamOffset", ""), OffsetsHashesArray.ParamNames.LAYERPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space2", "")));
                dataStream.WriteValueS16(short.Parse(xmlNav.GetAttribute("unknownA", "")));
                dataStream.WriteValueS16(short.Parse(xmlNav.GetAttribute("unknownB", "")));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space3", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("blendParamOffset", ""), OffsetsHashesArray.ParamNames.BLENDPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space4", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("clipParamOffset", ""), OffsetsHashesArray.ParamNames.CLIPPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space5", "")));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("Context", "")));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("Callback", "")));
            }
        }

        private void CMoveMotionMatching(XPathNavigator xmlNav = null)
        {
            if (xmlNav == null)
            {
                writer.WriteStartElement("CMoveMotionMatching");
                writer.WriteAttributeString("space0", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("uniqueID", dataStream.ReadValueU64().ToString());
                writer.WriteAttributeString("CriteriaValue", dataStream.ReadValueF32().ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("space1", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("layerParamOffset", GetHashFromOffset());
                writer.WriteAttributeString("space2", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("unknownA", dataStream.ReadValueS16().ToString());
                writer.WriteAttributeString("unknownB", dataStream.ReadValueS16().ToString());
                writer.WriteAttributeString("space3", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("blendParamOffset", GetHashFromOffset());
                writer.WriteAttributeString("space4", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("clipParamOffset", GetHashFromOffset());
                writer.WriteAttributeString("space5", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("motionmatchingParamOffset", GetHashFromOffset());
                writer.WriteAttributeString("space6", dataStream.ReadValueS32().ToString());
                if (isNewDawn || isFC6)
                {
                    writer.WriteAttributeString("moveblendParamOffset", GetHashFromOffset());
                    writer.WriteAttributeString("space7", dataStream.ReadValueS32().ToString());
                }
            }
            else
            {
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space0", "")));
                dataStream.WriteValueU64(ulong.Parse(xmlNav.GetAttribute("uniqueID", "")));
                dataStream.WriteValueF32(float.Parse(xmlNav.GetAttribute("CriteriaValue", ""), CultureInfo.InvariantCulture));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space1", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("layerParamOffset", ""), OffsetsHashesArray.ParamNames.LAYERPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space2", "")));
                dataStream.WriteValueS16(short.Parse(xmlNav.GetAttribute("unknownA", "")));
                dataStream.WriteValueS16(short.Parse(xmlNav.GetAttribute("unknownB", "")));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space3", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("blendParamOffset", ""), OffsetsHashesArray.ParamNames.BLENDPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space4", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("clipParamOffset", ""), OffsetsHashesArray.ParamNames.CLIPPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space5", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("motionmatchingParamOffset", ""), OffsetsHashesArray.ParamNames.MOTIONMATCHINGPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space6", "")));
                if (isNewDawn || isFC6)
                {
                    GetOffsetFromHash(xmlNav.GetAttribute("moveblendParamOffset", ""), OffsetsHashesArray.ParamNames.MOVEBLENDPARAM);
                    dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space7", "")));
                }
            }
        }

        private void CMoveFacialAnim(XPathNavigator xmlNav = null)
        {
            if (xmlNav == null)
            {
                writer.WriteStartElement("CMoveFacialAnim");
                writer.WriteAttributeString("space0", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("uniqueID", dataStream.ReadValueU64().ToString());
                writer.WriteAttributeString("CriteriaValue", dataStream.ReadValueF32().ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("space1", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("layerParamOffset", GetHashFromOffset());
                writer.WriteAttributeString("space2", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("unknownA", dataStream.ReadValueS16().ToString());
                writer.WriteAttributeString("unknownB", dataStream.ReadValueS16().ToString());
                writer.WriteAttributeString("space3", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("blendParamOffset", GetHashFromOffset());
                writer.WriteAttributeString("space4", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("clipParamOffset", GetHashFromOffset());
                writer.WriteAttributeString("space5", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("poseanimParamOffset", GetHashFromOffset());
                writer.WriteAttributeString("space6", dataStream.ReadValueS32().ToString());
            }
            else
            {
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space0", "")));
                dataStream.WriteValueU64(ulong.Parse(xmlNav.GetAttribute("uniqueID", "")));
                dataStream.WriteValueF32(float.Parse(xmlNav.GetAttribute("CriteriaValue", ""), CultureInfo.InvariantCulture));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space1", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("layerParamOffset", ""), OffsetsHashesArray.ParamNames.LAYERPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space2", "")));
                dataStream.WriteValueS16(short.Parse(xmlNav.GetAttribute("unknownA", "")));
                dataStream.WriteValueS16(short.Parse(xmlNav.GetAttribute("unknownB", "")));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space3", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("blendParamOffset", ""), OffsetsHashesArray.ParamNames.BLENDPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space4", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("clipParamOffset", ""), OffsetsHashesArray.ParamNames.CLIPPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space5", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("poseanimParamOffset", ""), OffsetsHashesArray.ParamNames.POSEANIMPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space6", "")));
            }
        }

        private void CMoveSequence(XPathNavigator xmlNav = null)
        {
            if (xmlNav == null)
            {
                writer.WriteStartElement("CMoveSequence");
                writer.WriteAttributeString("space0", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("uniqueID", dataStream.ReadValueU64().ToString());
                writer.WriteAttributeString("CriteriaValue", dataStream.ReadValueF32().ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("space1", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("layerParamOffset", GetHashFromOffset());
                writer.WriteAttributeString("space2", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("unknownA", dataStream.ReadValueS16().ToString());
                writer.WriteAttributeString("unknownB", dataStream.ReadValueS16().ToString());
                writer.WriteAttributeString("space3", dataStream.ReadValueS32().ToString());
            }
            else
            {
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space0", "")));
                dataStream.WriteValueU64(ulong.Parse(xmlNav.GetAttribute("uniqueID", "")));
                dataStream.WriteValueF32(float.Parse(xmlNav.GetAttribute("CriteriaValue", ""), CultureInfo.InvariantCulture));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space1", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("layerParamOffset", ""), OffsetsHashesArray.ParamNames.LAYERPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space2", "")));
                dataStream.WriteValueS16(short.Parse(xmlNav.GetAttribute("unknownA", "")));
                dataStream.WriteValueS16(short.Parse(xmlNav.GetAttribute("unknownB", "")));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space3", "")));
            }
        }

        private void CMoveAnimTechProgressToPMS(XPathNavigator xmlNav = null)
        {
            if (xmlNav == null)
            {
                writer.WriteStartElement("CMoveAnimTechProgressToPMS");
                writer.WriteAttributeString("space0", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("unknownA", dataStream.ReadValueF32().ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("space1", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("blendParamOffset1", GetHashFromOffset());
                writer.WriteAttributeString("space2", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("blendParamOffset2", GetHashFromOffset());
                writer.WriteAttributeString("space3", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("curveParamOffset", GetHashFromOffset());
                writer.WriteAttributeString("space4", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("pmsvalueParamOffset", GetHashFromOffset(true));
                writer.WriteAttributeString("unknownB", dataStream.ReadValueS16().ToString());
                writer.WriteAttributeString("space5", dataStream.ReadValueS32().ToString());
            }
            else
            {
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space0", "")));
                dataStream.WriteValueF32(float.Parse(xmlNav.GetAttribute("unknownA", ""), CultureInfo.InvariantCulture));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space1", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("blendParamOffset1", ""), OffsetsHashesArray.ParamNames.BLENDPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space2", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("blendParamOffset2", ""), OffsetsHashesArray.ParamNames.BLENDPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space3", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("curveParamOffset", ""), OffsetsHashesArray.ParamNames.CURVEPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space4", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("pmsvalueParamOffset", ""), OffsetsHashesArray.ParamNames.PMSVALUEPARAM, true);
                dataStream.WriteValueS16(short.Parse(xmlNav.GetAttribute("unknownB", "")));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space5", "")));
            }
        }

        private void CMoveMotionMatchingList(XPathNavigator xmlNav = null)
        {
            if (xmlNav == null)
            {
                writer.WriteStartElement("CMoveMotionMatchingList");
                writer.WriteAttributeString("space0", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("uniqueID", dataStream.ReadValueU64().ToString());
                writer.WriteAttributeString("CriteriaValue", dataStream.ReadValueF32().ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("space1", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("layerParamOffset", GetHashFromOffset());
                writer.WriteAttributeString("space2", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("unknownA", dataStream.ReadValueS16().ToString());
                writer.WriteAttributeString("unknownB", dataStream.ReadValueS16().ToString());
                writer.WriteAttributeString("space3", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("hash_26A3631F", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("space4", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("motionmatchingParamOffset", GetHashFromOffset());
                writer.WriteAttributeString("space5", dataStream.ReadValueS32().ToString());
            }
            else
            {
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space0", "")));
                dataStream.WriteValueU64(ulong.Parse(xmlNav.GetAttribute("uniqueID", "")));
                dataStream.WriteValueF32(float.Parse(xmlNav.GetAttribute("CriteriaValue", ""), CultureInfo.InvariantCulture));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space1", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("layerParamOffset", ""), OffsetsHashesArray.ParamNames.LAYERPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space2", "")));
                dataStream.WriteValueS16(short.Parse(xmlNav.GetAttribute("unknownA", "")));
                dataStream.WriteValueS16(short.Parse(xmlNav.GetAttribute("unknownB", "")));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space3", "")));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("hash_26A3631F", "")));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space4", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("motionmatchingParamOffset", ""), OffsetsHashesArray.ParamNames.MOTIONMATCHINGPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space5", "")));
            }
        }

        private void CMoveMotionMatchingPartialAnim(XPathNavigator xmlNav = null)
        {
            if (xmlNav == null)
            {
                writer.WriteStartElement("CMoveMotionMatchingPartialAnim");
                writer.WriteAttributeString("space0", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("uniqueID", dataStream.ReadValueU64().ToString());
                writer.WriteAttributeString("CriteriaValue", dataStream.ReadValueF32().ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("space1", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("layerParamOffset", GetHashFromOffset());
                writer.WriteAttributeString("space2", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("unknownA", dataStream.ReadValueS16().ToString());
                writer.WriteAttributeString("unknownB", dataStream.ReadValueS16().ToString());
                writer.WriteAttributeString("space3", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("animParamOffset", GetHashFromOffset());
                writer.WriteAttributeString("space4", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("clipParamOffset", GetHashFromOffset());
                writer.WriteAttributeString("space5", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("motionmatchinganimspeedParamOffset", GetHashFromOffset());
                writer.WriteAttributeString("space6", dataStream.ReadValueS32().ToString());
            }
            else
            {
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space0", "")));
                dataStream.WriteValueU64(ulong.Parse(xmlNav.GetAttribute("uniqueID", "")));
                dataStream.WriteValueF32(float.Parse(xmlNav.GetAttribute("CriteriaValue", ""), CultureInfo.InvariantCulture));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space1", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("layerParamOffset", ""), OffsetsHashesArray.ParamNames.LAYERPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space2", "")));
                dataStream.WriteValueS16(short.Parse(xmlNav.GetAttribute("unknownA", "")));
                dataStream.WriteValueS16(short.Parse(xmlNav.GetAttribute("unknownB", "")));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space3", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("animParamOffset", ""), OffsetsHashesArray.ParamNames.ANIMPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space4", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("clipParamOffset", ""), OffsetsHashesArray.ParamNames.CLIPPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space5", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("motionmatchinganimspeedParamOffset", ""), OffsetsHashesArray.ParamNames.MOTIONMATCHINGANIMSPEEDPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space6", "")));
            }
        }

        private void CMoveMotionMatchingAnim(XPathNavigator xmlNav = null)
        {
            if (xmlNav == null)
            {
                writer.WriteStartElement("CMoveMotionMatchingAnim");
                writer.WriteAttributeString("space0", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("uniqueID", dataStream.ReadValueU64().ToString());
                writer.WriteAttributeString("CriteriaValue", dataStream.ReadValueF32().ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("space1", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("layerParamOffset", GetHashFromOffset());
                writer.WriteAttributeString("space2", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("unknownA", dataStream.ReadValueS16().ToString());
                writer.WriteAttributeString("unknownB", dataStream.ReadValueS16().ToString());
                writer.WriteAttributeString("space3", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("animParamOffset", GetHashFromOffset());
                writer.WriteAttributeString("space4", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("motionmatchinganimspeedParamOffset", GetHashFromOffset());
                writer.WriteAttributeString("space5", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("motionmatchinganimmotionParamOffset", GetHashFromOffset());
                writer.WriteAttributeString("space6", dataStream.ReadValueS32().ToString());
            }
            else
            {
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space0", "")));
                dataStream.WriteValueU64(ulong.Parse(xmlNav.GetAttribute("uniqueID", "")));
                dataStream.WriteValueF32(float.Parse(xmlNav.GetAttribute("CriteriaValue", ""), CultureInfo.InvariantCulture));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space1", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("layerParamOffset", ""), OffsetsHashesArray.ParamNames.LAYERPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space2", "")));
                dataStream.WriteValueS16(short.Parse(xmlNav.GetAttribute("unknownA", "")));
                dataStream.WriteValueS16(short.Parse(xmlNav.GetAttribute("unknownB", "")));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space3", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("animParamOffset", ""), OffsetsHashesArray.ParamNames.ANIMPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space4", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("motionmatchinganimspeedParamOffset", ""), OffsetsHashesArray.ParamNames.MOTIONMATCHINGANIMSPEEDPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space5", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("motionmatchinganimmotionParamOffset", ""), OffsetsHashesArray.ParamNames.MOTIONMATCHINGANIMMOTIONPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space6", "")));
            }
        }

        private void CMoveMotionMatchingAnimGroup(XPathNavigator xmlNav = null)
        {
            if (xmlNav == null)
            {
                writer.WriteStartElement("CMoveMotionMatchingAnimGroup");
                writer.WriteAttributeString("space0", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("uniqueID", dataStream.ReadValueU64().ToString());
                writer.WriteAttributeString("CriteriaValue", dataStream.ReadValueF32().ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("space1", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("layerParamOffset", GetHashFromOffset());
                writer.WriteAttributeString("space2", dataStream.ReadValueS32().ToString());
                writer.WriteAttributeString("unknownA", dataStream.ReadValueS16().ToString());
                writer.WriteAttributeString("unknownB", dataStream.ReadValueS16().ToString());
                writer.WriteAttributeString("space3", dataStream.ReadValueS32().ToString());
            }
            else
            {
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space0", "")));
                dataStream.WriteValueU64(ulong.Parse(xmlNav.GetAttribute("uniqueID", "")));
                dataStream.WriteValueF32(float.Parse(xmlNav.GetAttribute("CriteriaValue", ""), CultureInfo.InvariantCulture));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space1", "")));
                GetOffsetFromHash(xmlNav.GetAttribute("layerParamOffset", ""), OffsetsHashesArray.ParamNames.LAYERPARAM);
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space2", "")));
                dataStream.WriteValueS16(short.Parse(xmlNav.GetAttribute("unknownA", "")));
                dataStream.WriteValueS16(short.Parse(xmlNav.GetAttribute("unknownB", "")));
                dataStream.WriteValueS32(int.Parse(xmlNav.GetAttribute("space3", "")));
            }
        }
    }
}
