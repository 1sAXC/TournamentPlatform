import { QueryClientProvider } from '@tanstack/react-query';
import { useEffect } from 'react';
import { queryClient } from './app/queryClient';
import { AppRouter } from './app/router';
import { ToastStack } from './shared/ui/Toast';
import { useAuthStore } from './shared/auth/authStore';
import { useMeSync } from './shared/auth/useMe';

function AuthBootstrap() {
  // Mounted inside QueryClientProvider so it can use TanStack Query.
  useMeSync();
  return null;
}

export function App() {
  const hydrate = useAuthStore((s) => s.hydrate);
  useEffect(() => { hydrate(); }, [hydrate]);

  return (
    <QueryClientProvider client={queryClient}>
      <AuthBootstrap />
      <AppRouter />
      <ToastStack />
    </QueryClientProvider>
  );
}
