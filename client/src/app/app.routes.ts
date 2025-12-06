import { Routes } from '@angular/router';
import { MemberList } from '../features/members/member-list/member-list';
import { MemberDeatiled } from '../features/members/member-deatiled/member-deatiled';
import { Home } from '../features/home/home';
import { Lists } from '../features/lists/lists';
import { Messages } from '../features/messages/messages';

export const routes: Routes = [
    { path: '', component: Home },
    { path: 'members', component: MemberList },
    { path: 'members/:id', component: MemberDeatiled },
    { path: 'list', component: Lists },
    { path: 'messages', component: Messages },
    { path: '**', component: Home }
];
