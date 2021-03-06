using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using GR.BusinessLogic.Models;

using System.Threading.Tasks;

namespace GR.BusinessLogic{
    public class Inventory{
        Hashtable inventoryListHashTable = new Hashtable();
        DatabaseContext databaseContext ;
        public Inventory(){
            databaseContext = new DatabaseContext();
        }

        private void LoadAllInventory(){
            // Refresh the Inventory List
            databaseContext = new DatabaseContext();    
        }

        public void EndTheDay(){
            LoadAllInventory();
            int qualityDegradation = 1;
            int sellInDegradation = 1;
           
            
            List<Item> itemList = databaseContext.Items.ToList();
            foreach (Item item in itemList){
                int qualtityMultiplier = 1;
                int sellInMultipler = 1;
                int maxQualtity = 50;
                
                 // set the sell in and quality multipliers based on the category
                switch (item.Category){
                    case "Food":
                        if (item.Name == "Aged Brie"){
                            // Increases in qiality as it get older
                            qualtityMultiplier = -1;    
                        }
                        break;
                    
                    case "Sulfuras":
                        // Legendary item - never decreases quality or sellin
                        //                - Qualtity can be higher than normal max quality
                        maxQualtity = item.Quality;
                        qualtityMultiplier = 0;
                        sellInMultipler = 0;
                        break;
                     
                    case "Backstage Passes":
                        // Increases in qiality as it get older
                        qualtityMultiplier = -1;
                        
                        if (item.SellIn <= 10){
                            qualtityMultiplier = -2;
                        }
                        if (item.SellIn <= 5){
                            qualtityMultiplier = -3;
                        }

                        // Once the Concert has passed, qualtiy drops to 0;
                        if (item.SellIn == 0){
                            item.Quality = 0;
                            qualtityMultiplier = 1;
                        }
                        break;
                    
                    case "Conjured":
                        // Decreases twice as fast as normal items
                        qualtityMultiplier = 2;
                        break;

                    default:
                        break;
                }

                // if SellIn has passed Item degrades twice as fast
                if (item.SellIn == 0){
                    qualtityMultiplier = 2;
                }

                item.Quality = item.Quality - qualityDegradation * qualtityMultiplier;
                item.SellIn = item.SellIn - sellInDegradation * sellInMultipler;

                // An Items Sell In and Qualtity can never be less than 0;
                if (item.Quality < 0){
                    item.Quality = 0;
                }

                if (item.SellIn < 0){
                    item.SellIn = 0;
                }

                // An Items Qualtity can never be more that the max qualtity
                if (item.Quality > maxQualtity){
                    item.Quality = maxQualtity;
                }
               
                // Update the database context
                databaseContext.Update(item);
            }

            // Save the results to the database
            databaseContext.SaveChanges();
        }

        public Item GetItem(string itemName){
            LoadAllInventory();
            Item item = databaseContext.Items.Find(itemName);
            return item;
        }

        public List<Item> GetAllItems(){
            LoadAllInventory();
            List<Item> itemList = databaseContext.Items.ToList();
            return itemList;
        }

        public List<Item> GetTrashList(){
            LoadAllInventory();
            List<Item> trashList = databaseContext.Items.Where(i => i.Quality == 0).ToList();
            return trashList;           
        }

        public void ImportInventory(){
            // Import Inventory form the Inventory.txt file
            string inventoryFile = "inventory.txt";
            StreamReader sr = null;
            LoadAllInventory();
            try{
                 sr = new StreamReader(inventoryFile);
                 int itemsImported = 0;
            
	            // Read the stream to a string, and write the string to the database.
                String line = sr.ReadLine();
                while(line != null){
                    itemsImported++;
                    Console.WriteLine(string.Format("Importing: {0}", line));
                    string[] itemInfo = line.Split(',');
                    Item item = new Item();
                    item.Name = itemInfo[0];
                    item.Category= itemInfo[1];
                    item.SellIn =Convert.ToInt32( itemInfo[2]);
                    item.Quality = Convert.ToInt32(itemInfo[3]);

                    this.databaseContext.Items.Add(item);

                    line = sr.ReadLine();

                }

                Console.WriteLine(string.Format("Imported {0} items from file", itemsImported));

                databaseContext.SaveChanges();

                Console.WriteLine("Changes saved to database");
            }
            catch(Exception ex){
                Console.WriteLine("Error Importing Inventory: {0}", ex.Message);
            }
            finally{
                if (sr != null)
                    sr.Dispose();
            }
        }
    }
}