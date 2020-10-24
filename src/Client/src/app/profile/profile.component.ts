import { Component, OnInit } from '@angular/core';
import { MsalService } from '@azure/msal-angular';
import { HttpClient } from '@angular/common/http';
import { InteractionRequiredAuthError, AuthError } from 'msal';

const GRAPH_ENDPOINT = 'https://graph.microsoft.com/v1.0/me';

@Component({
  selector: 'app-profile',
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.css']
})
export class ProfileComponent implements OnInit {
  profile;
  message;

  constructor(private authService: MsalService, private http: HttpClient) { }

  ngOnInit() {
    this.clearProfile();
  }
  onclick(event){
    let request = {
      scopes: ["https://owlvey.onmicrosoft.com/api/read"]
    };
    this.authService.acquireTokenSilent(request).then(resp=>{
      this.http.post("https://owlvey.azure-api.net/backoffice/profile/manual/paths/invoke", {
          name: 'test', email: 'gcvalderrama@hotmail.com'
        },
        {
         responseType: 'text',
         headers:{
            authorization: 'Bearer ' + resp.accessToken
         }
        }).subscribe(data=>{
          this.message = data;
          console.log(data);
        }, error=>{
        console.error(error);
      });

    }).catch(error => {
      console.error(error);
    });
  }
  clearProfile(){
    let request = {
      scopes: ["https://owlvey.onmicrosoft.com/api/read"]
    };
    this.authService.acquireTokenSilent(request).then(resp=>{
      console.log(resp);
      this.http.get("https://owlvey.azure-api.net/api/profile",
        {
         responseType: 'text',
         headers:{
            authorization: 'Bearer ' + resp.accessToken
         }
        }).subscribe(data=>{
          this.message = data;
          console.log(data);
        }, error=>{
        console.error(error);
      });

    }).catch(error => {
      console.error(error);
    });
  }

  getProfile() {

    this.http.get(GRAPH_ENDPOINT)
    .subscribe({
      next: (profile) => {
        this.profile = profile;
      },
      error: (err: AuthError) => {
        // If there is an interaction required error,
        // call one of the interactive methods and then make the request again.
        if (InteractionRequiredAuthError.isInteractionRequiredError(err.errorCode)) {
          this.authService.acquireTokenPopup({
            scopes: this.authService.getScopesForEndpoint(GRAPH_ENDPOINT)
          })
          .then(() => {
            this.http.get(GRAPH_ENDPOINT)
              .toPromise()
              .then(profile => {
                this.profile = profile;
              });
          });
        }
      }
    });
  }
}
