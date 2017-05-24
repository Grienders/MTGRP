﻿using System;
using MongoDB.Bson;
using RoleplayServer.resources.database_manager;
using System.Collections.Generic;

namespace RoleplayServer.resources.group_manager.lspd
{
    public class CriminalRecord
    {
        public static List<CriminalRecord> Crimes = new List<CriminalRecord>();

        public ObjectId Id { get; set; }

        public string CharacterId { get; set; }
        public string OfficerId { get; set; }
        public string CrimeName { get; set; }
        public DateTime DateTime { get; set; }
        public bool ActiveCrime { get; set; }

        public CriminalRecord(string characterId, string arrestingOfficerId, string crime, bool Activecrime)
        {
            CharacterId = characterId;
            OfficerId = arrestingOfficerId;
            CrimeName = crime;
            DateTime = DateTime.Now;
            ActiveCrime = Activecrime;
        }

        public void Insert()
        {
            DatabaseManager.CriminalRecordTable.InsertOne(this);
        }
    }
}
