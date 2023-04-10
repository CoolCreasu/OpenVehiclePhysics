# OpenVehiclePhysics
 OpenVehiclePhysics is an open source Unity project for vehicle physics.  
 The current Unity editor version is 2021.3.22f1  
 Contributions are welcome! Feel free to submit pull requests, raise issues, or provide feedback.  
 Your help is valuable in improving this project!

---
### Known implementation issues 
Current setup of cars can cause them to start sliding uncontrollably, most likely are the current slip/friction calculations insufficient.
The likely solution to this issue is to have a different slip/friction calculation for the wheel collider.
