﻿using GTANetworkServer;
using GTANetworkShared;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using RoleplayServer.core;
using RoleplayServer.player_manager;
using System;
using RoleplayServer.inventory;

namespace RoleplayServer.group_manager.lspd
{
    class Lspd : Script
    {

        public Lspd()
        {
            API.onResourceStart += StartLspd;
            API.onClientEventTrigger += API_onClientEventTrigger;
        }

        //LSPD Locations. TODO: MAKE IT WORK WITH MARKERZONE!!!!
        public static readonly Vector3 JailOne = new Vector3(458.0021f, -1001.581f, 24.91485f);
        public static readonly Vector3 JailTwo = new Vector3(458.7058f, -998.1188f, 24.91487f);
        public static readonly Vector3 JailThree = new Vector3(459.6695f, -994.0704f, 24.91487f);
        public static readonly Vector3 FreeJail = new Vector3(427.7434f, -976.0182f, 30.70999f);
        public readonly Vector3 JailPosOne = new Vector3(461.8065, -994.4086, 25.06443);
        public readonly Vector3 JailPosTwo = new Vector3(461.8065, -997.6583, 25.06443);
        public readonly Vector3 JailPosThree = new Vector3(461.8065, -1001.302, 25.06443);


        public LinkedList<GTANetworkServer.Object> Objects = new LinkedList<GTANetworkServer.Object>();


        public void StartLspd()
        {

        }


        private void API_onClientEventTrigger(Client player, string eventName, params object[] arguments)
        {
            switch (eventName)
            {
                case "LSPD_Menu_Change_Clothes":
                    {
                        Character c = API.getEntityData(player.handle, "Character");
                        
                        if (c.Group.CommandType != Group.CommandTypeLspd)
                        {
                            return;
                        }

                        c.IsInPoliceUniform = !c.IsInPoliceUniform;
                        ChatManager.AmeLabelMessage(player,
                            "changes into their " + (c.IsInPoliceUniform ? "police uniform." : "civilian clothing."), 8000);
                        c.update_ped();
                        c.Save();

                        break;
                    }
                case "LSPD_Menu_Toggle_Duty":
                    {
                        Character c = API.getEntityData(player.handle, "Character");

                        if (c.Group.CommandType != Group.CommandTypeLspd)
                        {
                            return;
                        }

                        c.IsOnPoliceDuty = !c.IsOnPoliceDuty;
                        GroupManager.SendGroupMessage(player,
                            c.CharacterName + " is now " + (c.IsOnPoliceDuty ? "on" : "off") + " police duty.");
                        c.Save();

                        break;
                    }
                case "LSPD_Menu_Equip_Standard_Equipment":
                    {
                        Character c = API.getEntityData(player.handle, "Character");

                        if (c.Group.CommandType != Group.CommandTypeLspd)
                        {
                            return;
                        }

                        GiveLspdEquipment(player);
                        API.sendChatMessageToPlayer(player, Color.White, "You have been given the standard LSPD equipment.");

                        break;
                    }
                case "LSPD_Menu_Equip_SWAT_Equipment":
                    {
                        Character c = API.getEntityData(player.handle, "Character");

                        if (c.Group.CommandType != Group.CommandTypeLspd)
                        {
                            return;
                        }

                        GiveLspdEquipment(player, 1);
                        API.sendChatMessageToPlayer(player, Color.White, "You have been given the standard SWAT equipment.");
                        break;
                    }
                case "toggle_megaphone_key":
                    {
                        megaphonetog_cmd(player);
                        break;
                    }
            }
        }
        [Command("recordcrime", GreedyArg = true)]
        public void recordcrimes_cmd(Client player, string id, string crimeid)
        {
            var receiver = PlayerManager.ParseClient(id);

            Character character = API.getEntityData(player.handle, "Character");
            Character receiverCharacter = API.getEntityData(receiver.handle, "Character");

            if (character.Group == Group.None || character.Group.CommandType != Group.CommandTypeLspd)
            {
                API.sendChatMessageToPlayer(player, Color.White, "You must be in the LSPD to use this command.");
                return;
            }

            if (receiver == null)
            {
                API.sendNotificationToPlayer(player, "~r~ERROR:~w~ Invalid player entered.");
                return;
            }

            var crime = Crime.Crimes.Find(c => c.Id == int.Parse(crimeid));

            if (crime == null)
            {
                API.sendChatMessageToPlayer(player, Color.White, "No crime with that ID exists.");
                return;
            }

            receiverCharacter.RecordCrime(character.CharacterName, crime);
            API.sendNotificationToPlayer(player, "You have recorded " + receiver.nametag + " for committing: " + crime.Name);
            API.sendNotificationToPlayer(receiver, player.nametag + " has recorded a crime you committed: ~r~" + crime.Name + "~w~.");
        }

        [Command("showcriminalrecord", GreedyArg = true)]
        public void criminalrecord_cmd(Client player, string id)
        {
            var receiver = PlayerManager.ParseClient(id);

            Character character = API.getEntityData(player.handle, "Character");
            Character receiverCharacter = API.getEntityData(receiver.handle, "Character");

            if (character.Group == Group.None || character.Group.CommandType != Group.CommandTypeLspd)
            {
                API.sendChatMessageToPlayer(player, Color.White, "You must be in the LSPD to use this command.");
                return;
            }

            if (receiver == null)
            {
                API.sendNotificationToPlayer(player, "~r~ERROR:~w~ Invalid player entered.");
                return;
            }

            API.sendChatMessageToPlayer(player, "=======CRIMIMNAL RECORD FOR " + receiverCharacter.CharacterName + "=======");
            foreach (var i in receiverCharacter.GetCriminalRecord())
            {
                if(i.ActiveCrime == true)
                {
                    API.sendChatMessageToPlayer(player, "~r~[ACTIVE] Type: " + i.Crime.Type + " Crime: " + i.Crime.Name + " Date issued: " + i.DateTime + " Recording officer: " + i.OfficerId);
                }

                else
                {
                    API.sendChatMessageToPlayer(player, "Type: " + i.Crime.Type + "Crime: " + i.Crime.Name + " Date issued: " + i.DateTime + " Recording officer: " + i.OfficerId);
                }
            }
        }

        [Command("listcrimes")]
        public void listcrimes_cmd(Client player)
        {
            Character character = API.getEntityData(player.handle, "Character");

            if (character.Group == Group.None || character.Group.CommandType != Group.CommandTypeLspd)
            {
                API.sendChatMessageToPlayer(player, Color.White, "You must be in the LSPD to use this command.");
                return;
            }

            foreach (var i in Crime.Crimes)
            {
                API.sendChatMessageToPlayer(player, i.Id + " | " + i.Type + " | " + i.Name + " | " + i.JailTime + " | " + i.Fine); //TODO: REPLACE WITH A MENU
            }
        }

        [Command("createcrime", GreedyArg=true)]
        public void createcrime_cmd(Client player, string type, int jailTime, int fine, string crimeName)
        {
            Character character = API.getEntityData(player.handle, "Character");


            if (character.Group == Group.None || character.Group.CommandType != Group.CommandTypeLspd)
            {
                API.sendChatMessageToPlayer(player, Color.White, "You must be in the LSPD to use this command.");
                return;
            }

            GroupManager.GroupCommandPermCheck(character, 7);

            if (Crime.CrimeExists(crimeName))
            {
                API.sendChatMessageToPlayer(player, "This crime already exists!");
                return;
            }
            Crime.InsertCrime(type, crimeName, jailTime, fine);
            API.sendChatMessageToPlayer(player, "Crime created and added to crime list.");
        }

        [Command("editcrime")]
        public void deletecrime_cmd(Client player, int id, string type, string crimeName, int jailTime, int fine)
        {
            Character character = API.getEntityData(player.handle, "Character");

            if (character.Group == Group.None || character.Group.CommandType != Group.CommandTypeLspd)
            {
                API.sendChatMessageToPlayer(player, Color.White, "You must be in the LSPD to use this command.");
                return;
            }

            GroupManager.GroupCommandPermCheck(character, 7);

            Crime crime = Crime.Crimes[id];
            crime.Type = type;
            crime.Name = crimeName;
            crime.JailTime = jailTime;
            crime.Fine = fine;
            crime.Update();
            API.sendChatMessageToPlayer(player, "Crime edited.");
        }

        [Command("deletecrime")]
        public void deletecrime_cmd(Client player, int id)
        {
            Character character = API.getEntityData(player.handle, "Character");

            if (character.Group == Group.None || character.Group.CommandType != Group.CommandTypeLspd)
            {
                API.sendChatMessageToPlayer(player, Color.White, "You must be in the LSPD to use this command.");
                return;
            }

            GroupManager.GroupCommandPermCheck(character, 7);

            Crime crimeDelete = Crime.Crimes[id];
            Crime.Crimes.Remove(crimeDelete);
            API.sendChatMessageToPlayer(player, "Crime deleted from crime list.");
        }

        [Command("wanted", GreedyArg = true)]
        public void wanted_cmd(Client player)
        {
            Character character = API.getEntityData(player.handle, "Character");
            if (character.Group == Group.None || character.Group.CommandType != Group.CommandTypeLspd)
            {
                API.sendChatMessageToPlayer(player, Color.White, "You must be in the LSPD to use this command.");
                return;
            }

            var players = API.getAllPlayers();

            API.sendChatMessageToPlayer(player, "---------------------WANTED LIST---------------------");


            foreach (var c in PlayerManager.Players)
            {
                if (c.HasActiveCriminalRecord() > 0)
                {
                    API.sendChatMessageToPlayer(player, c.CharacterName + " is wanted with " + c.HasActiveCriminalRecord() + " crimes.");
                }
            }
        }


        [Command("arrest", GreedyArg = true)]
        public void arrest_cmd(Client player, string id)
        {

            var receiver = PlayerManager.ParseClient(id);

            Character character = API.getEntityData(player.handle, "Character");
            Character receiverCharacter = API.getEntityData(receiver.handle, "Character");


            if (character.Group == Group.None || character.Group.CommandType != Group.CommandTypeLspd)
            {
                API.sendChatMessageToPlayer(player, Color.White, "You must be in the LSPD to use this command.");
                return;
            }

            if (receiver == player)
            {
                API.sendNotificationToPlayer(player, "~r~You can't arrest yourself!");
                return;
            }

            if (receiverCharacter.HasActiveCriminalRecord() == 0)
            {
                API.sendChatMessageToPlayer(player, "This player has no active crimes.");
            }
            
            if (character.GroupId == receiverCharacter.GroupId)
            {
                API.sendNotificationToPlayer(player, "~r~ERROR:~w~ You cannot arrest a member of the LSPD.");
                return;
            }

            if (receiver == null)
            {
                API.sendNotificationToPlayer(player, "~r~ERROR:~w~ Invalid player entered.");
                return;
            }

            if (player.position.DistanceTo(character.Group.ArrestLocation.Location) > 4)
            {
                API.sendChatMessageToPlayer(player, Color.White, "You are not at the arrest location.");
                return;
            }

            int time = 0;
            int fine = 0;

            var criminalRecord = receiverCharacter.GetCriminalRecord();

            foreach (var j in criminalRecord)
            {
                if (j.ActiveCrime == true)
                {
                    time += j.Crime.JailTime;
                    fine += j.Crime.Fine;
                    j.ActiveCrime = false;
                    j.Save();
                }
            }

            API.sendNotificationToPlayer(player, "You have arrested ~b~" + receiver.name + "~w~.");
            API.sendNotificationToPlayer(receiver, "You have been arrested by ~b~" + player.name + "~w~.");
            InventoryManager.DeleteInventoryItem(receiverCharacter, typeof(Money), fine);
            receiverCharacter.JailTimeLeft = time * 1000;
            JailControl(receiver, time);

        }

        [Command("release", GreedyArg = true)]
        public void release_cmd(Client player, string id)
        {

            var receiver = PlayerManager.ParseClient(id);

            Character character = API.getEntityData(player.handle, "Character");
            Character receiverCharacter = API.getEntityData(receiver.handle, "Character");

            if (character.Group == Group.None || character.Group.CommandType != Group.CommandTypeLspd)
            {
                API.sendChatMessageToPlayer(player, Color.White, "You must be in the LSPD to use this command.");
                return;
            }

            GroupManager.GroupCommandPermCheck(character, 3);

            if (receiverCharacter.IsJailed == false)
            {
                API.sendChatMessageToPlayer(player, "This player is not jailed.");
                return;
            }

            if (receiver == null)
            {
                API.sendNotificationToPlayer(player, "~r~ERROR:~w~ Invalid player entered.");
                return;
            }

            API.sendNotificationToPlayer(player, "You have released ~b~" + receiver.name + "~w~ from prison.");
            API.sendNotificationToPlayer(receiver, "You have been released from prison by ~b~" + player.name + "~w~.");
            SetFree(receiver);

        }

        [Command("frisk", GreedyArg = true)]
        public void frisk_cmd(Client player, string id)
        {
            var receiver = PlayerManager.ParseClient(id);

            Character character = API.getEntityData(player.handle, "Character");
            Character receivercharacter = API.getEntityData(receiver, "Character");
            
         
            if (receiver == null)
            {
                API.sendNotificationToPlayer(player, "~r~ERROR:~w~ Invalid player entered.");
                return;
            }

            if (receiver == player)
            {
                API.sendNotificationToPlayer(player, "~r~You can't frisk yourself!");
            }

            if (API.getEntityPosition(player).DistanceToSquared(API.getEntityPosition(receiver)) > 16f)
            {
                API.sendNotificationToPlayer(player, "~r~You're too far away!");
                return;
            }
            
            if (receivercharacter.AreHandsUp == true || receivercharacter.IsCuffed == true)
            {
            
                API.sendChatMessageToPlayer(player, "-------------PLAYER INVENTORY-------------");
                foreach (var item in receivercharacter.Inventory)
                {
                    API.sendChatMessageToPlayer(player, $"* ~r~{item.LongName}~w~[{item.CommandFriendlyName}] ({item.Amount})");
                }
                API.sendChatMessageToPlayer(player, "-------------PLAYER INVENTORY-------------");
            }
            API.sendChatMessageToPlayer(player, "Players must be cuffed or have their hands up before you can frisk them.");

        }

        [Command("backupbeacon", GreedyArg = true)]
        public void backupbeacon_cmd(Client player)
        {
            Character character = API.getEntityData(player.handle, "Character");

            if (character.Group == Group.None || character.Group.CommandType != Group.CommandTypeLspd)
            {
                API.sendChatMessageToPlayer(player, Color.White, "You must be in the LSPD to use this command.");
                return;
            }

            API.sendNotificationToPlayer(player, "~b~Backup beacon deployed~w~. Available officers have been notified.");
            GroupManager.SendGroupMessage(player, player.nametag + " has deployed a backup beacon. Use /acceptbeacon to accept.");

            character.BeaconSet = true;
            character.BeaconCreator = player; 
            character.BeaconResetTimer = new Timer { Interval = 60000 };
            character.BeaconResetTimer.Elapsed += delegate { ResetBeacon(player); };
            character.BeaconResetTimer.Start();

        }

        [Command("acceptbeacon", GreedyArg = true)]
        public void acceptbeacon_cmd(Client player)
        {
            Character character = API.shared.getEntityData(player.handle, "Character");
            var beaconCreator = player;

            if (character.Group == Group.None || character.Group.CommandType != Group.CommandTypeLspd)
            {
                API.sendChatMessageToPlayer(player, Color.White, "You must be in the LSPD to use this command.");
                return;
            }

            foreach (var c in PlayerManager.Players)
            {
                if (c.BeaconSet == false)
                {
                    API.sendChatMessageToPlayer(player, "There are no active beacons.");
                    return;
                }

                beaconCreator = c.BeaconCreator;

            }

            API.sendNotificationToPlayer(player, "~b~Beacon accepted~w~. A waypoint has been added to your map.");
            API.triggerClientEvent(player, "update_beacon", API.getEntityPosition(beaconCreator), beaconCreator);
        }

        [Command("megaphonetoggle", Alias = "mp", GreedyArg = true)]
        public void megaphonetog_cmd(Client player)
        {
            var playerPos = API.getEntityPosition(player);
            Character character = API.getEntityData(player.handle, "Character");

            if (character.Group == Group.None || character.Group.CommandType != Group.CommandTypeLspd)
            {
                API.sendChatMessageToPlayer(player, Color.White, "You must be in the LSPD to use this command.");
                return;
            }
            if (API.getEntityData(player, "MegaphoneStatus") != true)
            {
                API.sendNotificationToPlayer(player, "You are speaking through a megaphone", true);
                API.setEntityData(player, "MegaphoneStatus", true);
                var megaphone = API.createObject(API.getHashKey("prop_megaphone_01"), playerPos, new Vector3());
                API.attachEntityToEntity(megaphone, player, "IK_R_Hand", new Vector3(0, 0, 0), new Vector3(0, 0, 0));
                return;
            }
            API.sendNotificationToPlayer(player, "You are no longer speaking through a megaphone.");
            API.setEntityData(player, "MegaphoneStatus", false);
            API.deleteObject(player, playerPos, API.getHashKey("prop_megaphone_01"));
        }


        [Command("locker")]
        public void locker_cmd(Client player)
        {
            Character character = API.getEntityData(player.handle, "Character");

            if (character.Group == Group.None || character.Group.CommandType != Group.CommandTypeLspd)
            {
                API.sendChatMessageToPlayer(player, Color.White, "You must be in the LSPD to use this command.");
                return;
            }

            if (player.position.DistanceTo(character.Group.Locker.Location) > 8)
            {
                API.sendChatMessageToPlayer(player, Color.White, "You are not in the LSPD locker room.");
                return;
            }

            API.triggerClientEvent(player, "show_lspd_locker");
        }


        [Command("ticket")]
        public void ticket_cmd(Client player, string id, int amount)
        {
            var target = PlayerManager.ParseClient(id);

            Character character = API.getEntityData(player.handle, "Character");
            Character receiverCharacter = API.getEntityData(target, "Character");

            if (character.Group == Group.None || character.Group.CommandType != Group.CommandTypeLspd)
            {
                API.sendChatMessageToPlayer(player, Color.White, "You must be in the LSPD to use this command.");
                return;
            }

            if (target == null)
            {
                API.sendChatMessageToPlayer(player, Color.White, "That player is not connected.");
                return;
            }

            if (API.getEntityPosition(player).DistanceToSquared(API.getEntityPosition(target)) > 16f)
            {
                API.sendNotificationToPlayer(player, "~r~You're too far away!");
                return;
            }

            API.sendChatMessageToPlayer(target, player.name + " is offering to hand you a ticket. Use /acceptcopticket to accept it.");
            API.sendChatMessageToPlayer(player, "You offer to hand " + target.name + " a ticket.");
            receiverCharacter.SentTicketAmount = amount;
            receiverCharacter.SentTicket = true;
            character.TicketTimer = new Timer { Interval = 10000 };
            character.TicketTimer.Elapsed += delegate { ResetTicket(target); };
            character.TicketTimer.Start();

        }

        [Command("unpaidtickets", GreedyArg = false)]
        public void unpaidtickets_cmd(Client player, string id = null)
        {
            var target = PlayerManager.ParseClient(id);


            Character character = API.getEntityData(player.handle, "Character");


            if (target == null)
            {
                API.sendChatMessageToPlayer(player, "You have ~b~ " + character.UnpaidTickets + "~w~ unpaid tickets.");
                return;
            }

            if (character.Group == Group.None || character.Group.CommandType != Group.CommandTypeLspd)
            {
                API.sendChatMessageToPlayer(player, Color.White, "You must be in the LSPD to use this command.");
                return;
            }

            Character receiverCharacter = API.getEntityData(target, "Character");
            API.sendChatMessageToPlayer(player, receiverCharacter.CharacterName + " has ~b~ " + receiverCharacter.UnpaidTickets + "~w~ unpaid tickets.");
        }

        [Command("acceptcopticket", GreedyArg = true)]
        public void ticketaccept_cmd(Client player)
        {
            Character character = API.getEntityData(player.handle, "Character");

            if (character.SentTicket == true)
            {
                character.SentTicket = false;
                character.TicketBalance += character.SentTicketAmount;
                character.UnpaidTickets += 1;
                character.Save();
                API.sendChatMessageToPlayer(player, "Ticket accepted. Pay for this ticket at the main desk of the police station.");
           

            }
            else
            {
                API.sendChatMessageToPlayer(player, Color.White, "There are no active tickets to accept.");
            }

        }

        [Command("paycoptickets", GreedyArg = true)]
        public void paytickets_cmd(Client player)
        {
            Character character = API.getEntityData(player.handle, "Character");

            foreach (Group group in GroupManager.Groups)
            {
                if (group.CommandType == Group.CommandTypeLspd)
                {
                    if (player.position.DistanceTo(group.FrontDesk.Location) > 5)
                    {
                        API.sendNotificationToPlayer(player, "~r~You are not at the front desk of the police station.");
                        return;
                    }
                }
            }

            if (character.UnpaidTickets == 0)
            {
                API.sendNotificationToPlayer(player, "~r~You have no active tickets to pay for.");
                return;
            }

            if (character.TicketBalance > Money.GetCharacterMoney(character))
            {
                API.sendNotificationToPlayer(player, "~r~You cannot afford to pay for your tickets.");
                return;
            }

            API.sendNotificationToPlayer(player, "~r~Congratulations! Your tickets have been paid off.");
            InventoryManager.DeleteInventoryItem(character, typeof(Money), character.TicketBalance);
            character.Save();
            character.UnpaidTickets = 0;
        }

        [Command("deploy", GreedyArg = true)]
        public void deploy_cmd(Client player, string objectid)
        {
            Character character = API.getEntityData(player.handle, "Character");

            if (character.Group == Group.None || character.Group.CommandType != Group.CommandTypeLspd)
            {
                API.sendChatMessageToPlayer(player, Color.White, "You must be in the LSPD to use this command.");
                return;
            }

            GroupManager.GroupCommandPermCheck(character, 4);

            if (int.Parse(objectid) > 5)
            {
                API.sendChatMessageToPlayer(player, "Deployable items are between 1-5");
            }
            var playerpos = API.getEntityPosition(player);
            var playerrot = API.getEntityRotation(player);
            var playerDimension = API.getEntityDimension(player);

            switch (objectid)
            {
                case "1":
                    {
                        var item = API.createObject(API.getHashKey("prop_mp_barrier_01"), playerpos - new Vector3(0, 0, 1f), playerrot, playerDimension);
                        Objects.AddLast(item);
                        break;
                    }

                case "2":
                    {
                        var item = API.createObject(API.getHashKey("prop_barrier_wat_03b"), playerpos - new Vector3(0, 0, 1f), playerrot, playerDimension);
                        Objects.AddLast(item);
                        break;
                    }
                case "3":
                    {
                        var item = API.createObject(API.getHashKey("prop_barrier_work04a"), playerpos - new Vector3(0, 0, 1f), playerrot, playerDimension);
                        Objects.AddLast(item);
                        break;
                    }
                case "4":
                    {
                        var item = API.createObject(API.getHashKey("prop_mp_conc_barrier_01"), playerpos - new Vector3(0, 0, 1f), playerrot, playerDimension);
                        Objects.AddLast(item);
                        break;
                    }
                case "5":
                    {
                        var item = API.createObject(API.getHashKey("prop_barrier_work05"), playerpos - new Vector3(0, 0, 1f), playerrot, playerDimension);
                        Objects.AddLast(item);
                        break;
                    }
            }
            API.sendNotificationToPlayer(player, "Object placed. There are now ~r~" + Objects.Count + "~w~ objects placed.");
        }


        [Command("removelastobject", GreedyArg = true)]
        public void removeobject_cmd(Client player)
        {
            Character character = API.getEntityData(player.handle, "Character");

            if (character.Group == Group.None || character.Group.CommandType != Group.CommandTypeLspd)
            {
                API.sendChatMessageToPlayer(player, Color.White, "You must be in the LSPD to use this command.");
                return;
            }

            if (Objects.Count() == 0)
            {
                API.sendChatMessageToPlayer(player, "There are no more objects to remove.");
                return;
            }

            API.deleteEntity(Objects.Last());
            Objects.RemoveLast();
            API.sendNotificationToPlayer(player, "Object removed. There are now ~r~" + Objects.Count + "~w~ placed.");
        }

        [Command("removeallobjects", GreedyArg = true)]
        public void removeallobjects_cmd(Client player)
        {
            Character character = API.getEntityData(player.handle, "Character");

            if (character.Group == Group.None || character.Group.CommandType != Group.CommandTypeLspd)
            {
                API.sendChatMessageToPlayer(player, Color.White, "You must be in the LSPD to use this command.");
                return;
            }

            if (Objects.Count() == 0)
            {
                API.sendChatMessageToPlayer(player, "There are no more objects to remove.");
                return;
            }

            int len = Objects.Count();

            foreach (var i in Objects)
            {
                API.deleteEntity(i);
            }

            var node = Objects.First;

            while (node.Next != null)
            {
                var next = node.Next;
                Objects.Remove(node);
            }
            API.sendNotificationToPlayer(player, "~r~" + len + " objects removed.");
        }

        [Command("setlockerpos")]
        public void setlockerpos_cmd(Client player)
        {
            Character character = player.GetCharacter();

            GroupManager.GroupCommandPermCheck(character, 10);

            if (character.Group.Type != Group.CommandTypeLspd)
            {
                API.sendChatMessageToPlayer(player, Color.White, "Only the LSPD may use this command.");
                return;
            }

            if (character.Group.Locker == MarkerZone.None)
            {
                character.Group.Locker = new MarkerZone(character.Client.position, character.Client.rotation,
                    character.Client.dimension)
                { LabelText = "LSPD Locker Room~n~/locker" };
                character.Group.Save();
                character.Group.Locker.Create();
            }
            else
            {
                character.Group.Locker.Location = character.Client.position;
                character.Group.Locker.Rotation = character.Client.rotation;
                character.Group.Locker.Dimension = character.Client.dimension;
                character.Group.Locker.LabelText = "LSPD Locker Room~n~/locker";
                character.Group.Locker.Refresh();
                character.Group.Save();
            }

            API.sendChatMessageToPlayer(player, Color.White, "You have moved the LSPD locker location.");
            return;
        }

        [Command("setfrontdeskpos")]
        public void setfrontdeskpos_cmd(Client player)
        {
            Character character = player.GetCharacter();

            GroupManager.GroupCommandPermCheck(character, 10);

            if (character.Group.Type != Group.CommandTypeLspd)
            {
                API.sendChatMessageToPlayer(player, Color.White, "Only the LSPD may use this command.");
                return;
            }

            if (character.Group.FrontDesk == MarkerZone.None)
            {
                character.Group.FrontDesk = new MarkerZone(character.Client.position, character.Client.rotation,
                    character.Client.dimension)
                { LabelText = "LSPD Front Desk~n~/paycoptickets" };
                character.Group.Save();
                character.Group.FrontDesk.Create();
            }
            else
            {
                character.Group.FrontDesk.Location = character.Client.position;
                character.Group.FrontDesk.Rotation = character.Client.rotation;
                character.Group.FrontDesk.Dimension = character.Client.dimension;
                character.Group.FrontDesk.LabelText = "LSPD Front Desk~n~/paycoptickets";
                character.Group.FrontDesk.Refresh();
                character.Group.Save();
            }

            API.sendChatMessageToPlayer(player, Color.White, "You have moved the LSPD front desk location.");
            return;
        }

        [Command("setarrestpos")]
        public void setarrestpos_cmd(Client player)

        {
            Character character = player.GetCharacter();

            GroupManager.GroupCommandPermCheck(character, 10);

            if (character.Group.Type != Group.CommandTypeLspd)
            {
                API.sendChatMessageToPlayer(player, Color.White, "Only the LSPD may use this command.");
                return;
            }

            if (character.Group.ArrestLocation == MarkerZone.None)
            {
                character.Group.ArrestLocation = new MarkerZone(character.Client.position, character.Client.rotation,
                        character.Client.dimension)
                { LabelText = "Arrest Location~n~/arrest" };

                character.Group.ArrestLocation.Create();
            }
            else
            {
                character.Group.ArrestLocation.Location = character.Client.position;
                character.Group.ArrestLocation.Rotation = character.Client.rotation;
                character.Group.ArrestLocation.Dimension = character.Client.dimension;
                character.Group.ArrestLocation.LabelText = "Arrest Location~n~/arrest";
                character.Group.ArrestLocation.Refresh();
            }

            API.sendChatMessageToPlayer(player, Color.White, "You have moved the LSPD arrest location.");
        }

        public void GiveLspdEquipment(Client player, int type = 0)
        {
            API.removeAllPlayerWeapons(player);

            switch (type)
            {
                case 0:
                    API.givePlayerWeapon(player, WeaponHash.StunGun, 1, false, true);
                    API.givePlayerWeapon(player, WeaponHash.Nightstick, 1, false, true);
                    API.givePlayerWeapon(player, WeaponHash.Pistol, 250, false, true);
                    API.givePlayerWeapon(player, WeaponHash.Flashlight, 1, false, true);

                    API.setPlayerWeaponTint(player, WeaponHash.Pistol, WeaponTint.LSPD);
                    break;
                case 1:
                    API.givePlayerWeapon(player, WeaponHash.CombatPistol, 250, false, true);
                    API.givePlayerWeapon(player, WeaponHash.CombatPDW, 300, false, true);
                    API.givePlayerWeapon(player, WeaponHash.SmokeGrenade, 3, false, true);
                    API.givePlayerWeapon(player, WeaponHash.BZGas, 3, false, true);

                    API.setPlayerWeaponTint(player, WeaponHash.CombatPistol, WeaponTint.LSPD);
                    API.setPlayerWeaponTint(player, WeaponHash.CombatPDW, WeaponTint.LSPD);
                    break;
            }
            API.setPlayerHealth(player, 100);
            API.setPlayerArmor(player, 100);
        }

        public void ResetTicket(Client player)
        {
            Character character = API.getEntityData(player.handle, "Character");
            character.SentTicket = false;
            character.TicketTimer.Stop();
        }


        public void ResetBeacon(Client player)
        {
            Character character = API.getEntityData(player.handle, "Character");
            character.BeaconSet = false;
            character.BeaconResetTimer.Stop();
        }

        public static void JailControl(Client player, int seconds)
        {
            Character character = API.shared.getEntityData(player.handle, "Character");

            int jailOnePlayers = API.shared.getPlayersInRadiusOfPosition(3.7f, JailOne).Count;
            int jailTwoPlayers = API.shared.getPlayersInRadiusOfPosition(3.7f, JailTwo).Count;
            int jailThreePlayers = API.shared.getPlayersInRadiusOfPosition(3.7f, JailThree).Count;
            int smallest = API.shared.getAllPlayers().Count;
            int chosenCell = 0;
            List<int> list = new List<int>(new int[] { jailOnePlayers, jailTwoPlayers, jailThreePlayers });


            //Choose correct cell for player
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] < smallest)
                {
                    smallest = list[i];
                    chosenCell = i;
                }
            }

            if (chosenCell == 0)
                API.shared.setEntityPosition(player, JailOne);
            else if (chosenCell == 1)
                API.shared.setEntityPosition(player, JailTwo);
            else
                API.shared.setEntityPosition(player, JailThree);

            API.shared.removeAllPlayerWeapons(player);
            character.IsJailed = true;

            API.shared.sendChatMessageToPlayer(player, "You have been placed in jail for " + character.JailTimeLeft/60/1000 + " minutes.");

            character.JailTimeLeftTimer = new Timer { Interval = 1000 };
            character.JailTimeLeftTimer.Elapsed += delegate { UpdateTimer(player); };
            character.JailTimeLeftTimer.Start();
            character.JailTimer = new Timer { Interval = character.JailTimeLeft };
            character.JailTimer.Elapsed += delegate { SetFree(player); };
            character.JailTimer.Start();
        }

        public static void UpdateTimer(Client player)
        {
            Character character = API.shared.getEntityData(player.handle, "Character");
            character.JailTimeLeft -= 1000;
        }

        public static void SetFree(Client player)
        {
            Character character = API.shared.getEntityData(player.handle, "Character");
            if (character.IsJailed == false)
            {
                return;
            }
            character.JailTimeLeft = 0;
            API.shared.sendChatMessageToPlayer(player, "~b~You are free to go.");
            character.IsJailed = false;
            API.shared.setEntityPosition(player, FreeJail);
            character.JailTimer.Stop();
            character.JailTimeLeftTimer.Stop();

        }
    }
}
