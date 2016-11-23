function [connected_components, component_num]=connected_components_analysis(connection_matrix)
% According to the connection matrix, provide the connected components of
% the graph using Depth-first-searching(DFS) Method

nodes_num=size(connection_matrix,1);
component_num=0;
visited=zeros(nodes_num,1);
record_components=zeros;

for i=1:nodes_num
    temp_component=zeros;
    if visited(i)==1
        continue;
    else
        visited(i)=1;
        component_num=component_num+1;
        temp_component(1,1)=i;
        [visited,temp_component]=depth_first_search(connection_matrix,i,temp_component,visited);
        if i==1
            record_components=temp_component;
        else
            dimension=max(size(record_components,1),size(temp_component,1));
            record_components(dimension+1,1)=0;
            temp_component(dimension+1,1)=0;
            record_components=[record_components temp_component];
            
            if sum(record_components(end,:))==0
                record_components=record_components(1:end-1,:);
            end
        end
    end
end

connected_components=record_components;

end