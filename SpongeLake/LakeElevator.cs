using System.Collections.Generic;
using System.Linq;

namespace Quipcord.SpongeLake {
    public class LakeElevator {
        public string roomDesc;
        public List<LakeElevatorFloor> floors = new List<LakeElevatorFloor>();
        public int currentFloor = 0;
        public HashSet<int> dest = new HashSet<int>();
        public bool goingUp = true;
        public HashSet<int> requestsUp = new HashSet<int>();
        public HashSet<int> requestsDown = new HashSet<int>();
        public enum State {
            stopped, stationary, starting, moving, moved
        }
        public State state = State.stationary;
        public int timeUntilMove = 6;
        public bool moved = false;

        public LakeElevator() {

        }
        public LakeElevator(LakeRoom source) {
            roomDesc = source.description;
        }

        public void Update(LakeRoom room) {
            UpdateMain();
            UpdateRoom();
            void UpdateRoom() {
                if (moved) {
                    var floor = floors[currentFloor];
                    room.events.Add(net => {
                        foreach (var playerId in room.players) {
                            net.GetChannel(playerId).SendMessageAsync($"The elevator moves to **{floor.name}**");
                        }
                    });
                }
                if (state == LakeElevator.State.stopped) {
                    room.events.Add(net => {
                        foreach (var playerId in room.players) {
                            net.GetChannel(playerId).SendMessageAsync($"The elevator stops. The elevator door opens.");
                        }
                    });
                    var floor = floors[currentFloor];
                    room.exits["out"] = floor.exit;
                    room.description = string.IsNullOrWhiteSpace(roomDesc) ? floor.desc : $"{roomDesc} ({floor.desc})";
                }
                if (state == LakeElevator.State.starting) {
                    room.events.Add(net => {
                        foreach (var playerId in room.players) {
                            net.GetChannel(playerId).SendMessageAsync($"The elevator door closes. The elevator starts going {(goingUp ? "up" : "down")}");
                        }
                    });
                    room.exits.Remove("out");
                }
            }
            void UpdateMain() {
                moved = false;
                if (dest.Count() == 0 && requestsUp.Count() == 0 && requestsDown.Count() == 0) {
                    state = State.stationary;
                    timeUntilMove = 2;
                } else if (timeUntilMove > 0) {
                    timeUntilMove--;
                    if (state == State.stopped) {
                        state = State.stationary;
                    } else if (state == State.starting) {
                        state = State.moving;
                    }
                } else if (state == State.moving) {
                    if (goingUp) {
                        currentFloor++;
                    } else {
                        currentFloor--;
                    }
                    moved = true;
                    state = State.moved;
                    timeUntilMove = 2;
                } else {
                    UpdateMovement();
                }
                void UpdateMovement() {
                    if (goingUp) {

                        if (requestsUp.Contains(currentFloor)) {
                            requestsUp.Remove(currentFloor);
                            //Stop here
                            SetStopped();
                        }
                        if (dest.Contains(currentFloor)) {
                            dest.Remove(currentFloor);
                            //Stop here
                            SetStopped();
                        } else if (ShouldGoUp()) {
                            //Keep going up
                            SetMoving();
                        } else {
                            goingUp = false;
                            if (requestsDown.Contains(currentFloor)) {
                                //Stop here
                                SetStopped();
                            }
                        }
                    } else {
                        if (requestsDown.Contains(currentFloor)) {
                            requestsDown.Remove(currentFloor);
                            //Stop here
                            SetStopped();
                        }
                        if (dest.Contains(currentFloor)) {
                            dest.Remove(currentFloor);
                            //Stop here
                            SetStopped();
                        } else if (ShouldGoDown()) {
                            //Keep going down
                            SetMoving();
                        } else {
                            goingUp = true;
                            if (requestsUp.Contains(currentFloor)) {
                                //Stop here
                                SetStopped();
                            }
                        }
                    }
                }
            }
            void SetStopped() {
                state = State.stopped;
                timeUntilMove = 6;
            }
            void SetMoving() {
                if(state == State.moved) {
                    state = State.moving;
                    timeUntilMove = 0;
                    UpdateMain();
                } else if(state != State.moving) {
                    state = State.starting;
                    timeUntilMove = 2;
                }
            }
            bool ShouldGoUp() => dest.Any(f => f > currentFloor) || requestsUp.Any(f => f > currentFloor) || requestsDown.Any(f => f > currentFloor);
            bool ShouldGoDown() => dest.Any(f => f < currentFloor) || requestsUp.Any(f => f < currentFloor) || requestsDown.Any(f => f < currentFloor);
        }
        public class LakeElevatorFloor {
            public NetExit exit;
            public string name;
            public string desc;
        }
    }
}
