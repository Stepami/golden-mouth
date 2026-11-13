import type {ReactNode} from 'react';

const shapes={
 trash:<><path d="M3 6h18M9 6V4a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v2M5 6l1 14a1 1 0 0 0 1 1h10a1 1 0 0 0 1-1l1-14M10 10v7M14 10v7"/></>,
 preferences:<><path d="M4 7h7m4 0h5M4 17h3m4 0h9"/><circle cx="13" cy="7" r="2"/><circle cx="9" cy="17" r="2"/></>,
 home:<><path d="m3 10 9-7 9 7v10a1 1 0 0 1-1 1h-5v-7H9v7H4a1 1 0 0 1-1-1Z"/></>,
 feed:<><rect x="4" y="3" width="16" height="18" rx="3"/><path d="M8 8h8M8 12h8M8 16h5"/></>,
 search:<><circle cx="10.5" cy="10.5" r="6.5"/><path d="m16 16 5 5"/></>,
 profile:<><circle cx="12" cy="8" r="4"/><path d="M4 21v-2a8 8 0 0 1 16 0v2"/></>,
 goals:<><circle cx="12" cy="12" r="9"/><path d="m8 12 3 3 5-6"/></>,
 compose:<><rect x="3" y="3" width="18" height="18" rx="5"/><path d="M12 7v10M7 12h10"/></>,
 messages:<><path d="M21 11.5A8.5 8.5 0 0 1 12.5 20H4l-2 2V11.5A9.5 9.5 0 0 1 21 11.5Z"/><path d="M7 10h10M7 14h6"/></>,
 activity:<><path d="M18 8a6 6 0 0 0-12 0c0 7-3 7-3 9h18c0-2-3-2-3-9ZM10 21h4"/></>,
 heart:<path d="M20.5 5.5a5.2 5.2 0 0 0-7.4 0L12 6.6l-1.1-1.1a5.2 5.2 0 0 0-7.4 7.4L12 21l8.5-8.1a5.2 5.2 0 0 0 0-7.4Z"/>,
 comment:<path d="M21 11.5a9 9 0 0 1-9 9H3l1.7-4A9 9 0 1 1 21 11.5Z"/>,
 quote:<><path d="m16 3 4 4-4 4M20 7H6a3 3 0 0 0-3 3v2M8 21l-4-4 4-4M4 17h14a3 3 0 0 0 3-3v-2"/></>,
 more:<><circle cx="5" cy="12" r="1" fill="currentColor"/><circle cx="12" cy="12" r="1" fill="currentColor"/><circle cx="19" cy="12" r="1" fill="currentColor"/></>,
 arrow:<><path d="M6 18 18 6M6 6h12v12"/></>,
 back:<path d="m14 5-7 7 7 7"/>,
 close:<path d="m6 6 12 12M6 18 18 6"/>,
 flame:<path d="M12 3c1 4-3 5-3 8a3 3 0 0 0 5 2c1-1 1-3 1-4 3 3 4 5 4 7a7 7 0 0 1-14 0c0-4 3-7 7-13Z"/>,
 chevron:<path d="m6 9 6 6 6-6"/>,
 retry:<><path d="M20 5v5h-5M20 10a8 8 0 1 0-1 8"/></>,
 check:<path d="m5 12 4 4L19 6"/>,
} satisfies Record<string,ReactNode>;
export type LineIconName=keyof typeof shapes;
export function LineIcon({name}:{name:LineIconName}){
 return <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">{shapes[name]}</svg>;
}
