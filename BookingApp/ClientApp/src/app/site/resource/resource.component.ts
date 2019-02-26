import { Component, OnInit } from '@angular/core';

import { ResourceService } from '../../services/resource.service';
import { Resource } from '../../models/resource';
import { ActivatedRoute, Router } from '@angular/router';
import { Logger } from '../../services/logger.service';
import { AuthService } from '../../services/auth.service';
import { Observable } from 'rxjs/Observable';
import 'rxjs/add/operator/catch';

@Component({
  selector: 'app-resource',
  templateUrl: './resource.component.html',
  styleUrls: ['./resource.component.css']
})
export class ResourceComponent implements OnInit {

  resource: Resource;
  id: number;

  constructor(
    private folderService: ResourceService,
    private route: ActivatedRoute,
    private authService: AuthService,
    private router: Router
  ) {
    authService.AuthChanged.subscribe(() => {
      this.resetData();
    });
  }

  ngOnInit() {
    this.route.params.subscribe(params => {
      this.id = +params['id'];
    });

    this.resetData();
  }

  resetData() {
    this.folderService.getResource(this.id).subscribe((response: Resource) => {
      this.resource = response;
    }, error => { this.router.navigate(['/error']); });
  }
}