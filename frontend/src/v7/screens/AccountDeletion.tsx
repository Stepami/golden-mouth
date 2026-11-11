import {useRef,useState} from 'react';
import {Button,Dialog,Notice} from '../ui';
export function AccountDeletion({onDelete,onClose}:{onDelete:()=>Promise<void>;onClose:()=>void}){
 const locked=useRef(false),[busy,setBusy]=useState(false),[error,setError]=useState('');
 const remove=async()=>{if(locked.current)return;locked.current=true;setBusy(true);setError('');try{await onDelete();}catch{setError('Не удалось подтвердить удаление. Проверь подключение и попробуй снова.');}finally{locked.current=false;setBusy(false);}};
 return <Dialog title="Удалить аккаунт?" onClose={()=>{if(!locked.current)onClose();}}><p>Удалятся аккаунт, миры, записи и переписка. Вернуть их будет нельзя.</p>{error&&<Notice title="Удаление не подтверждено" tone="error">{error}</Notice>}<Button className="account-delete-confirm" busy={busy} onClick={()=>void remove()}>Удалить аккаунт навсегда</Button><Button variant="secondary" disabled={busy} onClick={onClose}>Отмена</Button></Dialog>;
}
