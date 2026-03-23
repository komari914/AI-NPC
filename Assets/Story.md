**Project Aurora - Homicide Case File**

_Internal Game Design Document · Not for Distribution_

**① Case Overview**

**Setting:** A mid-sized software company. Late evening, after most staff have left.

**Victim:** Daniel - Project Lead of Project Aurora.

**Cause of death:** Blunt force trauma to the back of the head. The weapon is a solid metal desk award (a 'Project Excellence' trophy from a previous release), found wiped clean and returned to Daniel's desk. The medical examiner places time of death between 10:00 PM and 11:00 PM.

**Background:** During the final months of Project Aurora's development, Daniel personally rejected several subsystem design proposals and took sole credit for the final design document, which was built substantially on others' contributions. On the night of the incident, he stayed late to prepare materials for an upcoming internal review. Security logs show three colleagues were also in or near the building during the window of death.

**Correct Answer**

**Alex is the killer.** The taxi receipt (A2) is a fabricated alibi - it is missing the Late Night Fee surcharge that appears on all platform rides after 9:00 PM (confirmed by the PM's receipt P2), and the Wi-Fi reconnection log (A3) proves Alex's device was back inside the building at 10:23 PM, well within the time-of-death window. The marked design draft (A1) establishes both motive and emotional state.

**② Suspects**

**Alex - Senior Systems Designer ⚠ KILLER**

**Role in the project:** Alex led the early systems architecture phase. The original core concept for Aurora was Alex's design.

**Motives**

• **Credit theft:** Daniel incorporated Alex's key ideas into the final design document without attribution. Alex discovered this during a document review three weeks before the incident, and confronted Daniel about it - Daniel dismissed the complaint.

• **Blocked promotion:** Alex applied for a Senior Lead promotion six months ago. Daniel, who has final approval authority, has not acted on it. In a performance review two weeks earlier, Daniel noted 'concerns about Alex's collaborative attitude.'

• **Public humiliation:** The night of the incident, Daniel rejected Alex's final proposal in front of other team members, calling it 'derivative.' The two then had a private confrontation in Daniel's office.

**Alibi and its collapse**

• Alex claims to have taken a taxi home at 9:47 PM (A2).

• The receipt is missing the platform's standard Late Night Fee surcharge, which is automatically applied to all rides dispatched after 9:00 PM - confirmed by cross-referencing the PM's receipt (P2) from the same night.

• The Wi-Fi log (A3) shows Alex's registered laptop reconnecting to the office network at 10:23 PM - 23 minutes into the time-of-death window. Alex cannot explain this.

**Project Manager - PM**

**Role in the project:** Responsible for timelines, resources, and team coordination across all workstreams.

**Motives**

• **Ongoing power struggle:** Daniel and the PM clashed repeatedly over budget authority. Daniel had recently gone over the PM's head to secure additional development resources, embarrassing the PM in front of senior management.

• **Timeline pressure:** The PM blamed Daniel for scope creep that put the project behind schedule, affecting the PM's performance evaluation.

**Alibi**

• The PM holds a taxi receipt (P2) showing departure from a nearby office complex at 9:14 PM - the receipt includes an itemised Late Night Fee, a platform-standard surcharge for all rides after 9:00 PM. This detail becomes significant when cross-referenced with A2.

• However, P3 (a calendar confirmation email with a connection log) conclusively establishes the PM was in a remote video call with an external partner from 10:00 PM to 11:15 PM.

• The PM is a red herring. Strong motive, weak opportunity.

**Junior Programmer - JP**

**Role in the project:** Recently hired, still on probation. Assigned to a low-visibility subsystem.

**Motives**

• **Verbal abuse:** Daniel publicly belittled the JP's code in team stand-ups on multiple occasions. Colleagues noticed the JP became withdrawn after each incident.

• **Fear of termination:** The JP recently learned (through a colleague) that Daniel submitted a negative mid-cycle review, potentially threatening the probation period.

**The Red Herring Chain (JP appears suspicious - then is eliminated)**

• **J1:** The JP was still clocked in past 11:00 PM - in the building during the window of death.

• **J2:** Some code commit from 10-11 PM.

• **J3:** A Chat log showed that each submission made by this programmer is revised based on the designer's requirement. It is impossible for programmer to finish the work and kill a person in that time period.

**③ Evidence**

**Color key:** Blue = Alex | Green = PM | Orange = Junior Programmer | Purple = Scene / Shared

| **A1**<br><br>\[ALEX\] | **Marked Design Draft**<br><br>A printed subsystem design draft with handwritten annotations. Sections are crossed out with notes such as 'this part was taken' and 'no credit - again.' The handwriting matches Alex's known documents. Establishes motive: intellectual credit dispute and long-term grievance. |
| ---------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |

| **A2**<br><br>\[ALEX\] | **Taxi Ride Receipt**<br><br>A digital receipt showing a taxi departure from the office at 9:47 PM, destination listed as a residential address. The route distance is similar to the PM's receipt (P2). However, the fare contains no "Late Night Fee" surcharge - which should appear on any platform ride dispatched after 9:00 PM, as confirmed by P2. A player who cross-references with P2 will identify this as fabricated; A3 provides independent corroboration. |
| ---------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |

| **A3**<br><br>\[ALEX\] | **Internal Wi-Fi Reconnection Log**<br><br>A network access log showing Alex's registered laptop device reconnecting to the office Wi-Fi at 10:23 PM - inside the time-of-death window. Directly contradicts A2. This is the key piece of physical evidence. Combined with A1 (motive) and the inconsistency in A2, it closes the case against Alex. |
| ---------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |

| **P1**<br><br>\[PM\] | **Meeting Room Argument Record**<br><br>Handwritten notes from a heated internal planning meeting earlier that week. Documents a serious dispute between Daniel and the PM over budget and timeline. One entry reads: 'unilateral decisions will not stand.' Establishes PM motive but is not time-specific to the night of the incident. |
| -------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |

| **P2**<br><br>\[PM\] | **Late-Night Taxi Receipt**<br><br>A digital taxi receipt showing the PM departed from a nearby office complex at 9:14 PM, heading to a residential district. The route and distance are comparable to Alex's receipt (A2). Notably, the fare includes an itemised "Late Night Fee" surcharge - a standard charge automatically applied by the taxi platform for all rides dispatched after 9:00 PM. Alex's receipt (A2) covers a similar route at 9:47 PM yet shows no such surcharge, exposing it as fabricated. |
| -------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |

| **P3**<br><br>\[PM\] | **Remote Meeting Confirmation Email**<br><br>An automatically generated calendar confirmation and a separate video platform connection log. Confirms the PM joined a remote call with an external partner at 10:02 PM; the call ended at 11:17 PM. Provides a verified, timestamped alibi covering the entire time-of-death window. Eliminates the PM. |
| -------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |

| **J1**<br><br>\[JP\] | **Late Overtime Record**<br><br>A building access and HR overtime log. The JP badged in at 8:30 PM and remained clocked in past 11:00 PM. Establishes that the JP was in the building during the window of death. Does not by itself indicate proximity to Daniel's office. |
| -------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |

| **J2**<br><br>\[JP\] | **Task Submission Timestamp**<br><br>A version control system commit log. Shows some code submission by the JP at 10:08 PM, 10:37 PM, and 10:59 PM originating from the shared developer floor workstation area. The content shows that there's no time for JP to kill since the updated content are wrote according to the new requirement from designer. |
| -------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |

| **J3**<br><br>\[JP\] | **Designer - JP text record**<br><br>A Chat log showed that each submission made by this programmer is revised based on the designer's requirement. It is impossible for programmer to finish the work and kill a person in that time period. |
| -------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |

**④ Intended Solution Path**

The evidence is designed so that a player who inspects everything and reasons carefully will arrive at Alex through the following chain:

• **A1 →** Alex has a clear, documented motive (credit dispute + blocked promotion).

• **A2 →** Alex appears to have an alibi (left before 10 PM).

• **J1 →** JP has motive and time - suspicion shifts to JP.

• **J3 + J2 →** JP's proximity is explained; JP was working. Suspicion lifts.

• **P3 →** PM has a verified remote meeting alibi; eliminated.

• **A3 + A2 × P2 (missing Late Night Fee) →** Alex's alibi collapses. Only suspect without a verifiable alibi, with motive and confirmed presence.

• **Conclusion →** Alex is the killer.

**⑤ Sample NPC Dialogue - Both Persona Conditions**

The following illustrates how the two personas handle the same investigative moments. Factual content is identical; communication style differs.

**Exchange 1 - After inspecting A1 (Design Draft)**

**Participant:** _"What does this draft mean? Should I be looking at Alex?"_

**Empathic NPC:** _"That's a sharp observation. The annotations suggest someone felt their work was taken without credit - that's a meaningful grievance. It's worth keeping Alex in mind as you look at the other evidence."_

**Task-focused NPC:** _"The draft indicates uncredited use of design content. Potential motive for Alex. Continue gathering evidence."_

**Exchange 2 - After inspecting J3 (Keycard Log)**

**Participant:** _"The junior programmer was right outside Daniel's office. Does that mean they did it?"_

**Empathic NPC:** _"I can see why that would worry you - it definitely puts them in a suspicious location. But before drawing conclusions, it's worth asking: is there any other reason they might have been in that corridor? See if the other evidence tells you something."_

**Task-focused NPC:** _"Keycard access places the junior programmer in the corridor at 9:52 PM. Cross-reference with J4 before concluding."_

**Exchange 3 - After finding A3 collapses A2**

**Participant:** _"Wait - Alex's device was back in the building? But the taxi receipt said they left."_

**Empathic NPC:** _"Exactly - you caught it. The Wi-Fi log and the receipt can't both be true. That's a significant contradiction. What does that tell you about the alibi?"_

**Task-focused NPC:** _"Contradiction confirmed. The Wi-Fi log overrides the receipt. Alex's alibi is invalidated."_

**⑥ Design Notes**

**NPC knowledge boundary**

The NPC mentor has access to all evidence items but does not volunteer the solution. It responds to what the player asks. In Empathic condition: validates reasoning, asks guiding questions, acknowledges emotional content. In Task-focused condition: states facts, flags logical implications, does not comment on emotional dimensions.