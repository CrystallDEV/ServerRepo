using System.Collections.Generic;
using UnityEngine;

namespace Network.Teams
{
    public class TeamController : MonoBehaviour
    {
        private static TeamController instance;
        private readonly List<Team> teams = new List<Team>();

        public static TeamController getInstance()
        {
            return instance;
        }

        private void Awake()
        {
            //Check if instance already exists
            if (instance == null)
                instance = this;
            else if (instance != this)
                Destroy(gameObject);

            teams.Add(new Red(1));
            teams.Add(new Blue(2));

            DontDestroyOnLoad(gameObject);
        }

        public bool AddToTeam(ClientData _client, int team)
        {
            return AddToTeam(_client, GetTeamById(team));
        }

        public bool AddToTeam(ClientData _client, Team team)
        {
            if (team.AddPlayer(_client))
            {
                _client.Team = team;
                return true;
            }

            return false;
        }

        public bool RemoveFromTeam(ClientData _client)
        {
            return _client.Team.RemovePlayer(_client);
        }

        public Team GetTeamById(int id)
        {
            return teams[id - 1];
        }
    }
}