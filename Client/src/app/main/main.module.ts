import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';

import { MainRoutingModule } from './main-routing.module';
import { MainComponent } from './components/main/main.component';
import { SharedModule } from '../shared/shared.module';
import { CookiesDeclarationComponent } from './components/cookies-declaration/cookies-declaration.component';
import { PricingComponent } from './components/pricing/pricing.component';
import { SearchComponent } from './components/search/search.component';

@NgModule({
  declarations: [MainComponent, CookiesDeclarationComponent, PricingComponent, SearchComponent],
  imports: [CommonModule, MainRoutingModule, SharedModule],
})
export class MainModule {}
