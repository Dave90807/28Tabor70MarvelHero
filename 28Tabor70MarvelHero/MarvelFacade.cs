﻿using _28Tabor70MarvelHero.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;


namespace _28Tabor70MarvelHero
{
    public class MarvelFacade
    {
        private const string PrivateKey = "f7a4b4dd7ef782466f9f97e186b520dcbda8e29c";
        private const string PublicKey = "2aa01b9d67aeb7ee57df7572c0bcf5ba";
        private const int MaxCharacters = 1500;
        private const string ImageNotAvailablePath = "http://i.annihil.us/u/prod/marvel/i/mg/b/40/image_not_available";

        public static async Task PopulateMarvelCharactersAsync(ObservableCollection<Character> marvelCharacters)
        {
            try
            {
            var characterDataWrapper = await GetCharacterDataWrapperAsync();
            var characters = characterDataWrapper.data.results;
            foreach (var character in characters)
                {
                    // Filter characters that are missing thumbnail images

                    if (character.thumbnail != null
                            && character.thumbnail.path != ""
                            && character.thumbnail.path != ImageNotAvailablePath)
                    {
                        character.thumbnail.small = String.Format("{0}/standard_small.{1}",
                            character.thumbnail.path,
                            character.thumbnail.extension);

                        character.thumbnail.large = String.Format("{0}/portrait_xlarge.{1}",
                            character.thumbnail.path,
                            character.thumbnail.extension);

                        marvelCharacters.Add(character);
                    }
                }
            }
            catch (Exception)
            {
                return;
            }
        }

        public static async Task PopulateMarvelComicsAsync(int characterId, ObservableCollection<ComicBook> marvelComics)
        {
            try
            {
                var comicDataWrapper = await GetComicDataWrapperAsync(characterId);
                var comics = comicDataWrapper.data.results;
                foreach (var comic in comics)
                {
                    // Filter characters that are missing thumbnail images
                    if (comic.thumbnail != null
                        && comic.thumbnail.path != ""
                        && comic.thumbnail.path != ImageNotAvailablePath)
                    {
                        comic.thumbnail.small = String.Format("{0}/portrait_medium.{1}",
                            comic.thumbnail.path,
                            comic.thumbnail.extension);

                        comic.thumbnail.large = String.Format("{0}/portrait_xlarge.{1}",
                            comic.thumbnail.path,
                            comic.thumbnail.extension);
                        marvelComics.Add(comic);
                    }
                }
            }
            catch (Exception)
            {
                return;
            }
        }


        private static async Task<CharacterDataWrapper> GetCharacterDataWrapperAsync()
        {
            // Assemble the URL
            Random random = new Random();
            var offset = random.Next(1500);


            var timeStamp = DateTime.Now.Ticks.ToString();
            var hash = CreateHash(timeStamp);
            string url = String.Format("http://gateway.marvel.com:80/v1/public/characters?limit=10&offset={0}",
                offset);

            // Call to Marvel
            var jsonMessage = await CallMarvelAsync(url);

            // Deserialize the JSON - datacontract.json
            var serializer = new DataContractJsonSerializer(typeof(CharacterDataWrapper));
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(jsonMessage));

            var result = (CharacterDataWrapper)serializer.ReadObject(ms);
            return result;
        }

        private static async Task<ComicDataWrapper> GetComicDataWrapperAsync(int characterId)
        {
            var url = String.Format("http://gateway.marvel.com:80/v1/public/comics?characters={0}&limit=10",
                characterId);

            var jsonMessage = await CallMarvelAsync(url);

            // Response -> string / json -> deserialize
            var serializer = new DataContractJsonSerializer(typeof(ComicDataWrapper));
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(jsonMessage));

            var result = (ComicDataWrapper)serializer.ReadObject(ms);
            return result;
        }

        private async static Task<string> CallMarvelAsync(string url)
        {
            // Get the MD5 Hash
            var timeStamp = DateTime.Now.Ticks.ToString();
            var hash = CreateHash(timeStamp);

            string completeUrl = String.Format("{0}&apikey={1}&ts={2}&hash={3}", url, PublicKey, timeStamp, hash);

            // Call out to Marvel
            HttpClient http = new HttpClient();
            var response = await http.GetAsync(completeUrl);
            return await response.Content.ReadAsStringAsync();
        }

        private static string CreateHash(string timeStamp)  //returns the Hash
        {
            var toBeHashed = timeStamp + PrivateKey + PublicKey;
            var hashedMessage = ComputeMD5(toBeHashed);
            return hashedMessage;
        }
        // From Website search on how to create an Md5 hash.  It is not explained but takes a string "str"
        // and outputs the hash "res"
        private static string ComputeMD5(string str)
        {
            var alg = HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Md5);
            IBuffer buff = CryptographicBuffer.ConvertStringToBinary(str, BinaryStringEncoding.Utf8);
            var hashed = alg.HashData(buff);
            var res = CryptographicBuffer.EncodeToHexString(hashed);
            return res;
        }
    }
}
