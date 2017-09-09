using Lidgren.Network;
using System.Collections;
using System.Linq;
using UnityEngine;

partial class Server : MonoBehaviour {

    //or merge all 3 into one
    private void CalculatePlayerMovement () {
        foreach (ClientData _client in clients.Values.ToList()) {
            if (_client != null) {
                if (clientsTransform.ContainsKey(_client.ID)) {
                    Vector3 dir = Vector3.zero;
                    switch (_client.MoveDir) {
                        case MoveDirs.UP:
                            dir = Vector3.forward;
                            break;
                        case MoveDirs.UPRIGHT:
                            dir = (Vector3.forward + Vector3.right);
                            break;
                        case MoveDirs.RIGHT:
                            dir = Vector3.right;
                            break;
                        case MoveDirs.RIGHTDOWN:
                            dir = (Vector3.back + Vector3.right);
                            break;
                        case MoveDirs.DOWN:
                            dir = Vector3.back;
                            break;
                        case MoveDirs.DOWNLEFT:
                            dir = (Vector3.back + Vector3.left);
                            break;
                        case MoveDirs.LEFT:
                            dir = Vector3.left;
                            break;
                        case MoveDirs.LEFTUP:
                            dir = (Vector3.left + Vector3.forward);
                            break;
                        case MoveDirs.NONE:
                            dir = Vector3.zero;
                            break;
                    }

                    if (dir == Vector3.zero)
                        continue;

                    //position calculation
                    _client.LastPosition = _client.Position;
                    _client.animationState = AnimationStates.WALK;
                    
                    Transform player = clientsTransform[_client.ID];
                    player.rotation = Quaternion.Euler(_client.Rotation);

                    player.Translate(dir.normalized * _client.Speed * Time.deltaTime, Space.World);
                    _client.Position = player.transform.position;
                    
                }
            }
        }
    }

    public IEnumerator SpawnPlayer (ClientData _client) {        
        Transform player = Instantiate(playerPrefab);
        player.name = _client.ID.ToString();
        clientsTransform.Add(_client.ID, player);

        if (_client.Team == 1) {
            player.position = redBase.position;
        } else if (_client.Team == 2) {
            player.position = blueBase.position;
        }
        yield return null;
    }

    public IEnumerator DespawnPlayer(GameObject toDestroy) {
        Destroy(toDestroy);
        yield return null;
    }

    //data 1 = attacker, data 2 = attacked
    public static void CalculatePlayerHitpoints (ClientData attacker, ClientData defender) {
        //TODO -> Test for different options, if the attack is possible etc. , maybe new Class / method
        if (Vector3.Distance(attacker.Position, defender.Position) > 10) {
            return;
        }
        if (attacker.Team == defender.Team) {
            Debug.Log("Player: " + attacker.ID + " cant hit player: " + defender.ID + " because they are in the same team");
            return;
        }

        Debug.Log("Player " + attacker.ID + " damaged Player " + defender.ID + " for 10 DMG");
        if (defender.CurrentHitpoints - 10 < 0) {
            defender.IsDead = true;
            defender.CurrentHitpoints = 0;
            //data2.DeathTime = ((int) gameData.RoundTime / 100) + 20;
            //data2.AnimationState = 4;
            //Debug.Log("Player: " + data2.ID + " has died. Deathtime: " + data2.DeathTime);
            Debug.Log("Player: " + defender.ID + " has died.");

        } else {
            defender.CurrentHitpoints -= 10;
        }
    }

}
